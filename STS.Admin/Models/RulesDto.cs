namespace Sts.Admin.Models;

public class RulesSectionDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }
    public List<RulesPostDto> Posts { get; set; } = [];

    public RulesSectionDto Clone() => new()
    {
        Id = Id,
        Title = Title,
        Order = Order,
        Posts = Posts.Select(p => p.Clone()).ToList(),
    };
}

public class RulesPostDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    public RulesPostDto Clone() => new() { Id = Id, Title = Title, Content = Content };
}
