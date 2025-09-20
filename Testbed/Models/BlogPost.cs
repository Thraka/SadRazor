namespace Testbed.Models;

public class BlogPost
{
    public string Title { get; set; } = "Test Post";
    public string Author { get; set; } = "Test Author";
    public DateTime Date { get; set; } = DateTime.Now;
    public string Content { get; set; } = "This is a test post content.";
    public string[] Tags { get; set; } = new[] { "test", "markdown", "razor" };
}