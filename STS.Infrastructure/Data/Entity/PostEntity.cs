namespace Sts.Infrastructure.Data.Entities;

public sealed class PostEntity
{
    public string Id { get; set; } = string.Empty;
    public string SectionId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    public SectionEntity Section { get; set; } = null!;
}
