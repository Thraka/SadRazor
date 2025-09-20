using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace SadRazorEngine.Tests
{
    public class PartialIndentationTests
    {
        [Fact]
        public async Task Partial_ExplicitIndent_PrefixesNonEmptyLines()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            var partial = Path.Combine(tempDir, "partial.md");
            var root = Path.Combine(tempDir, "root.md");

            // Partial with leading blank line and two non-empty lines
            await File.WriteAllTextAsync(partial, "\nItemA\nItemB");

            await File.WriteAllTextAsync(root, $"# Root\n@(await PartialAsync(\"partial.md\", options: new SadRazorEngine.Runtime.PartialOptions {{ IndentAmount = 4 }}))\n");

            var engine = new SadRazorEngine.TemplateEngine();
            var result = await engine.LoadTemplate(root).RenderAsync();

            // Leading blank line should be preserved (no extra indentation on the blank line)
            Assert.Contains("\n\n    ItemA", result.Content);
            // Non-empty lines should be prefixed with four spaces
            Assert.Contains("    ItemA", result.Content);
            Assert.Contains("    ItemB", result.Content);

            Directory.Delete(tempDir, true);
        }

        [Fact]
        public async Task Partial_InheritColumn_FromListItem_ProducesNestedList()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            var partial = Path.Combine(tempDir, "partial.md");
            var root = Path.Combine(tempDir, "root.md");

            await File.WriteAllTextAsync(partial, "item1\nitem2");

            // Call partial in a list item so the partial should be indented to align with the list marker
            await File.WriteAllTextAsync(root, "- @(await PartialAsync(\"partial.md\", options: new SadRazorEngine.Runtime.PartialOptions { InheritColumn = true }))\n");

            var engine = new SadRazorEngine.TemplateEngine();
            var result = await engine.LoadTemplate(root).RenderAsync();

            // Expect the partial's first line to be prefixed by two spaces (one for the space after '-')
            Assert.Contains("-   item1", result.Content);
            Assert.Contains("  item2", result.Content);

            Directory.Delete(tempDir, true);
        }
    }
}
