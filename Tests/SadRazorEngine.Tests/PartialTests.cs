using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace SadRazorEngine.Tests
{
    public class PartialTests
    {
        [Fact]
        public async Task IncludeInlinesPartial()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            var partial = Path.Combine(tempDir, "_partial.md");
            var root = Path.Combine(tempDir, "root.md");

            await File.WriteAllTextAsync(partial, "- Partial line");

            var rootContent = """
# Title
@include "_partial.md"
End
""";
            await File.WriteAllTextAsync(root, rootContent);

            var engine = new SadRazorEngine.TemplateEngine();
            var result = await engine.LoadTemplate(root).RenderAsync();

            Assert.Contains("Partial line", result.Content);

            Directory.Delete(tempDir, true);
        }

        [Fact]
        public async Task PartialAsync_RendersPartialWithModel()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            var partial = Path.Combine(tempDir, "partial.md");
            var root = Path.Combine(tempDir, "root.md");

            await File.WriteAllTextAsync(partial, """
# Partial: @Model.Value
""");

            await File.WriteAllTextAsync(root, """
# Root
@(await PartialAsync("partial.md", new { Value = "X" }))
""");

            var engine = new SadRazorEngine.TemplateEngine();
            var result = await engine.LoadTemplate(root).RenderAsync();

            Assert.Contains("Partial: X", result.Content);

            Directory.Delete(tempDir, true);
        }

        [Fact]
        public async Task NestedIncludesResolveCorrectly()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            var subdir = Path.Combine(tempDir, "sub");
            Directory.CreateDirectory(subdir);

            var inner = Path.Combine(subdir, "inner.md");
            var main = Path.Combine(tempDir, "main.md");
            var root = Path.Combine(tempDir, "root.md");

            await File.WriteAllTextAsync(inner, "Inner content");
            await File.WriteAllTextAsync(main, """
@include "sub/inner.md"
""");
            await File.WriteAllTextAsync(root, """
@include "main.md"
""");

            var engine = new SadRazorEngine.TemplateEngine();
            var result = await engine.LoadTemplate(root).RenderAsync();

            Assert.Contains("Inner content", result.Content);

            Directory.Delete(tempDir, true);
        }

        [Fact]
        public async Task PartialAsync_ResolvesRelativeToTemplateBase()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            var dir = Path.Combine(tempDir, "dir");
            Directory.CreateDirectory(dir);

            var partial = Path.Combine(dir, "p.md");
            var root = Path.Combine(dir, "root.md");

            await File.WriteAllTextAsync(partial, "PartialHere");
            await File.WriteAllTextAsync(root, """
# Root
@(await PartialAsync("p.md"))
""");

            var engine = new SadRazorEngine.TemplateEngine();
            var result = await engine.LoadTemplate(root).RenderAsync();

            Assert.Contains("PartialHere", result.Content);

            Directory.Delete(tempDir, true);
        }
    }
}
