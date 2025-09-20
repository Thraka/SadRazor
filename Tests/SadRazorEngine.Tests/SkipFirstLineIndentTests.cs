using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using SadRazorEngine.Runtime;

namespace SadRazorEngine.Tests
{
    public class SkipFirstLineIndentTests
    {
        [Fact]
        public async Task Direct_NoIndent_InheritColumn_DefaultBehavior()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            var partial = Path.Combine(tempDir, "partial.md");
            var root = Path.Combine(tempDir, "root.md");

            await File.WriteAllTextAsync(partial, "Line1\nLine2\nLine3");

            // Direct call at column 0
            await File.WriteAllTextAsync(root, "@(await PartialAsync(\"partial.md\", options: new SadRazorEngine.Runtime.PartialOptions { InheritColumn = true }))");

            var engine = new SadRazorEngine.TemplateEngine();
            var result = await engine.LoadTemplate(root).RenderAsync();

            // Should have no indentation since we're at column 0
            Assert.Contains("Line1", result.Content);
            Assert.Contains("Line2", result.Content);
            Assert.Contains("Line3", result.Content);
            // No indentation expected
            Assert.StartsWith("Line1\n", result.Content.Replace("\r", ""));

            Directory.Delete(tempDir, true);
        }

        [Fact]
        public async Task Direct_TwoSpaces_InheritColumn_DefaultBehavior()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            var partial = Path.Combine(tempDir, "partial.md");
            var root = Path.Combine(tempDir, "root.md");

            await File.WriteAllTextAsync(partial, "Line1\nLine2\nLine3");

            // Two spaces before @(Partial...)
            await File.WriteAllTextAsync(root, "  @(await PartialAsync(\"partial.md\", options: new SadRazorEngine.Runtime.PartialOptions { InheritColumn = true }))");

            var engine = new SadRazorEngine.TemplateEngine();
            var result = await engine.LoadTemplate(root).RenderAsync();

            // Should indent all lines by 2 spaces
            Assert.Contains("  Line1", result.Content);
            Assert.Contains("  Line2", result.Content);
            Assert.Contains("  Line3", result.Content);

            Directory.Delete(tempDir, true);
        }

        [Fact]
        public async Task Direct_TwoSpaces_InheritColumn_SkipFirstLine()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            var partial = Path.Combine(tempDir, "partial.md");
            var root = Path.Combine(tempDir, "root.md");

            await File.WriteAllTextAsync(partial, "Line1\nLine2\nLine3");

            // Two spaces before @(Partial...) with SkipFirstLineIndent
            await File.WriteAllTextAsync(root, "  @(await PartialAsync(\"partial.md\", options: new SadRazorEngine.Runtime.PartialOptions { InheritColumn = true, SkipFirstLineIndent = true }))");

            var engine = new SadRazorEngine.TemplateEngine();
            var result = await engine.LoadTemplate(root).RenderAsync();

            // First line should not be indented, others should be indented by 2 spaces
            Assert.Contains("Line1", result.Content);
            Assert.Contains("  Line2", result.Content);
            Assert.Contains("  Line3", result.Content);
            // First line should immediately follow the two spaces
            Assert.Contains("  Line1\n", result.Content.Replace("\r", ""));

            Directory.Delete(tempDir, true);
        }

        [Fact]
        public async Task Inline_AfterText_InheritColumn_DefaultBehavior()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            var partial = Path.Combine(tempDir, "partial.md");
            var root = Path.Combine(tempDir, "root.md");

            await File.WriteAllTextAsync(partial, "Line1\nLine2\nLine3");

            // After some text - this simulates the user's scenario more accurately
            await File.WriteAllTextAsync(root, "Prefix: @(await PartialAsync(\"partial.md\", options: new SadRazorEngine.Runtime.PartialOptions { InheritColumn = true, SkipFirstLineIndent = false }))");

            var engine = new SadRazorEngine.TemplateEngine();
            var result = await engine.LoadTemplate(root).RenderAsync();

            // Default behavior: shows the bug where first line gets double-indented
            // This demonstrates the issue the user reported
            Assert.Contains("Prefix:         Line1", result.Content);  // First line incorrectly gets extra spaces
            Assert.Contains("        Line2", result.Content);  // Subsequent lines correctly indented by 8 spaces
            Assert.Contains("        Line3", result.Content);

            Directory.Delete(tempDir, true);
        }

        [Fact]
        public async Task Inline_AfterText_InheritColumn_SkipFirstLine()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            var partial = Path.Combine(tempDir, "partial.md");
            var root = Path.Combine(tempDir, "root.md");

            await File.WriteAllTextAsync(partial, "Line1\nLine2\nLine3");

            // After some text with SkipFirstLineIndent - this should solve the user's issue
            await File.WriteAllTextAsync(root, "Prefix: @(await PartialAsync(\"partial.md\", options: new SadRazorEngine.Runtime.PartialOptions { InheritColumn = true, SkipFirstLineIndent = true }))");

            var engine = new SadRazorEngine.TemplateEngine();
            var result = await engine.LoadTemplate(root).RenderAsync();

            // First line should not be indented (directly after "Prefix: "), others indented by 8 spaces
            Assert.Contains("Prefix: Line1", result.Content);  // First line directly appended
            Assert.Contains("        Line2", result.Content);  // Subsequent lines indented by 8 spaces
            Assert.Contains("        Line3", result.Content);

            Directory.Delete(tempDir, true);
        }

        [Fact]
        public async Task ListItem_InheritColumn_DefaultBehavior()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            var partial = Path.Combine(tempDir, "partial.md");
            var root = Path.Combine(tempDir, "root.md");

            await File.WriteAllTextAsync(partial, "Line1\nLine2\nLine3");

            // List item context - reproduces the existing test scenario
            await File.WriteAllTextAsync(root, "- @(await PartialAsync(\"partial.md\", options: new SadRazorEngine.Runtime.PartialOptions { InheritColumn = true, SkipFirstLineIndent = false }))");

            var engine = new SadRazorEngine.TemplateEngine();
            var result = await engine.LoadTemplate(root).RenderAsync();

            // Should indent all lines by 2 spaces (after "- ")
            Assert.Contains("  Line1", result.Content);
            Assert.Contains("  Line2", result.Content);
            Assert.Contains("  Line3", result.Content);
            // This creates the nested list structure: "- " + "  Line1" = "-   Line1"
            Assert.Contains("-   Line1", result.Content);

            Directory.Delete(tempDir, true);
        }

        [Fact]
        public async Task ListItem_InheritColumn_SkipFirstLine()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            var partial = Path.Combine(tempDir, "partial.md");
            var root = Path.Combine(tempDir, "root.md");

            await File.WriteAllTextAsync(partial, "Line1\nLine2\nLine3");

            // List item with SkipFirstLineIndent
            await File.WriteAllTextAsync(root, "- @(await PartialAsync(\"partial.md\", options: new SadRazorEngine.Runtime.PartialOptions { InheritColumn = true, SkipFirstLineIndent = true }))");

            var engine = new SadRazorEngine.TemplateEngine();
            var result = await engine.LoadTemplate(root).RenderAsync();

            // First line should be directly after "- ", others indented by 2 spaces
            Assert.Contains("- Line1", result.Content);
            Assert.Contains("  Line2", result.Content);
            Assert.Contains("  Line3", result.Content);

            Directory.Delete(tempDir, true);
        }

        [Fact]
        public async Task ExplicitIndent_SkipFirstLine_ShouldStillIndentFirst()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            var partial = Path.Combine(tempDir, "partial.md");
            var root = Path.Combine(tempDir, "root.md");

            await File.WriteAllTextAsync(partial, "Line1\nLine2\nLine3");

            // When using explicit IndentAmount, SkipFirstLineIndent should still work
            await File.WriteAllTextAsync(root, "Prefix: @(await PartialAsync(\"partial.md\", options: new SadRazorEngine.Runtime.PartialOptions { IndentAmount = 4, SkipFirstLineIndent = true }))");

            var engine = new SadRazorEngine.TemplateEngine();
            var result = await engine.LoadTemplate(root).RenderAsync();

            // First line should not be indented, others should be indented by 4 spaces
            Assert.Contains("Prefix: Line1", result.Content);
            Assert.Contains("    Line2", result.Content);
            Assert.Contains("    Line3", result.Content);

            Directory.Delete(tempDir, true);
        }

        [Fact]
        public async Task Sync_Partial_SkipFirstLine_ShouldWork()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            var partial = Path.Combine(tempDir, "partial.md");
            var root = Path.Combine(tempDir, "root.md");

            await File.WriteAllTextAsync(partial, "Line1\nLine2\nLine3");

            // Using synchronous Partial method
            await File.WriteAllTextAsync(root, "Prefix: @(Partial(\"partial.md\", options: new SadRazorEngine.Runtime.PartialOptions { InheritColumn = true, SkipFirstLineIndent = true }))");

            var engine = new SadRazorEngine.TemplateEngine();
            var result = await engine.LoadTemplate(root).RenderAsync();

            // First line should not be indented, others should be indented
            Assert.Contains("Prefix: Line1", result.Content);
            Assert.Contains("        Line2", result.Content);
            Assert.Contains("        Line3", result.Content);

            Directory.Delete(tempDir, true);
        }
    }
}