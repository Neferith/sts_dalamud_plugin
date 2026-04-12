using Microsoft.EntityFrameworkCore;
using Sts.Domain.Content;
using Sts.Domain.Content.DataSources;
using Sts.Infrastructure.Data;
using Sts.Infrastructure.Data.Entities;

namespace Sts.Infrastructure.DataSources;

/// <summary>
/// Implémentation SQLite de <see cref="IRulesDataSource"/> via EF Core.
/// </summary>
public sealed class SqliteRulesDataSource(StsDbContext db) : IRulesDataSource
{
    /// <inheritdoc/>
    public async Task<List<RulesSection>> LoadAsync()
    {
        var sections = await db.Sections
            .AsNoTracking()
            .Include(s => s.Posts)
            .OrderBy(s => s.Order)
            .ToListAsync();

        return sections.Select(ToModel).ToList();
    }

    /// <inheritdoc/>
    public async Task SaveAsync(List<RulesSection> sections)
    {
        var existing = await db.Sections
            .Include(s => s.Posts)
            .ToListAsync();

        var incomingIds = sections.Select(s => s.Id).ToHashSet();

        // Supprimer les sections absentes — cascade supprime leurs posts
        db.Sections.RemoveRange(existing.Where(e => !incomingIds.Contains(e.Id)));

        foreach (var section in sections)
        {
            var entity = existing.FirstOrDefault(e => e.Id == section.Id);

            if (entity is null)
            {
                db.Sections.Add(ToEntity(section));
            }
            else
            {
                entity.Title = section.Title;
                entity.Order = section.Order;

                var incomingPostIds = section.Posts.Select(p => p.Id).ToHashSet();

                db.Posts.RemoveRange(entity.Posts.Where(p => !incomingPostIds.Contains(p.Id)));

                foreach (var post in section.Posts)
                {
                    var postEntity = entity.Posts.FirstOrDefault(p => p.Id == post.Id);
                    if (postEntity is null)
                    {
                        entity.Posts.Add(new PostEntity
                        {
                            Id = post.Id,
                            SectionId = section.Id,
                            Title = post.Title,
                            Content = post.Content,
                        });
                    }
                    else
                    {
                        postEntity.Title = post.Title;
                        postEntity.Content = post.Content;
                    }
                }
            }
        }

        await db.SaveChangesAsync();
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static RulesSection ToModel(SectionEntity e) => new()
    {
        Id = e.Id,
        Title = e.Title,
        Order = e.Order,
        Posts = e.Posts.Select(p => new RulesPost
        {
            Id = p.Id,
            Title = p.Title,
            Content = p.Content,
        }).ToList(),
    };

    private static SectionEntity ToEntity(RulesSection s) => new()
    {
        Id = s.Id,
        Title = s.Title,
        Order = s.Order,
        Posts = s.Posts.Select(p => new PostEntity
        {
            Id = p.Id,
            SectionId = s.Id,
            Title = p.Title,
            Content = p.Content,
        }).ToList(),
    };
}
