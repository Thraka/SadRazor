using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SadRazorEngine.Tests
{
    public class AdditionalTemplateTests
    {
        [Fact]
        public async Task CompileFromString_RendersCorrectly_WithAnonymousModel()
        {
            var template = "# @Model.Title\n@Model.Content";
            var compiler = new SadRazorEngine.Compilation.RazorTemplateCompiler();
            var compiled = await compiler.CompileAsync(template);
            var context = new SadRazorEngine.Core.Models.TemplateContext(compiled);

            var result = await context.WithModel(new { Title = "Anon", Content = "AnonBody" }).RenderAsync();

            Assert.Contains("Anon", result.Content);
            Assert.Contains("AnonBody", result.Content);
        }

        [Fact]
        public async Task CompileFromString_RendersCorrectly_WithTypedModel()
        {
            var template = "@model SadRazorEngine.Tests.TypedModel\n# @Model.Title\n@Model.Content";
            var compiler = new SadRazorEngine.Compilation.RazorTemplateCompiler();
            var compiled = await compiler.CompileAsync(template, typeof(TypedModel));
            var context = new SadRazorEngine.Core.Models.TemplateContext(compiled);

            var model = new TypedModel { Title = "Typed", Content = "TypedBody" };

            var result = await context.WithModel(model).RenderAsync();

            Assert.Contains("Typed", result.Content);
            Assert.Contains("TypedBody", result.Content);
        }

        [Fact]
        public void LoadTemplate_MissingFile_Throws()
        {
            var engine = new SadRazorEngine.TemplateEngine();
            var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".md");
            var ex = Assert.Throws<FileNotFoundException>(() => engine.LoadTemplate(missing));
            Assert.Contains("Template file not found", ex.Message);
        }

        [Fact]
        public async Task CompiledTemplate_CanRenderConcurrently_WithPerCallModel()
        {
            var template = "# @Model.Value";
            var compiler = new SadRazorEngine.Compilation.RazorTemplateCompiler();
            var compiled = await compiler.CompileAsync(template);

            var tasks = Enumerable.Range(0, 20).Select(async i =>
            {
                var content = await compiled.RenderAsync(new { Value = i });
                return content;
            }).ToArray();

            var results = await Task.WhenAll(tasks);

            for (int i = 0; i < results.Length; i++)
            {
                Assert.Contains(i.ToString(), results[i]);
            }
        }
    }

    public class TypedModel
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
