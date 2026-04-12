namespace Sts.Admin.Models;

public sealed class ImageDto
{
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int SizeKb { get; set; }
}
