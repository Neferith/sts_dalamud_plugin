namespace Sts.Infrastructure.Data.Entities;

public sealed class SectionEntity
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }

    public List<PostEntity> Posts { get; set; } = [];
}
