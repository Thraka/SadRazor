using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace SadRazorEngine.Tests
{
    public class TemplateEngineTests
    {
        [Fact]
        public void CanConstructEngine()
        {
            var engine = new SadRazorEngine.TemplateEngine();
            Assert.NotNull(engine);
        }

        [Fact]
        public async Task RendersSimpleTemplate_WithDynamicModel()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".md");
            var template = """
# @Model.Title
@Model.Content
""";
            await File.WriteAllTextAsync(tempPath, template);

            var engine = new SadRazorEngine.TemplateEngine();

            var result = await engine.LoadTemplate(tempPath)
                .WithModel(new { Title = "Hello", Content = "Body" })
                .RenderAsync();

            Assert.Contains("Hello", result.Content);
            Assert.Contains("Body", result.Content);

            File.Delete(tempPath);
        }
    }
}
