using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace SadRazorEngine.Runtime
{
    public static class TemplateHelpers
    {
        private static string? ResolvePath(string path)
        {
            if (Path.IsPathRooted(path) && File.Exists(path))
                return path;

            var candidates = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), path),
                Path.Combine(AppContext.BaseDirectory, path),
                Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location) ?? string.Empty, path),
                Path.Combine(Path.GetDirectoryName(typeof(TemplateHelpers).Assembly.Location) ?? string.Empty, path)
            };

            foreach (var c in candidates)
            {
                if (File.Exists(c))
                    return c;
            }

            return null;
        }

        /// <summary>
        /// Synchronously renders a partial by resolving the provided <paramref name="path"/> and
        /// returning the rendered content. If <paramref name="options"/> contains an
        /// explicit <see cref="PartialOptions.IndentAmount"/>, the returned content will be
        /// indented by that amount. The <see cref="PartialOptions.InheritColumn"/> flag is not
        /// supported from this static helper because the helper cannot observe the calling
        /// template's writer state; callers should use the instance helper on
        /// <see cref="TemplateBase{TModel}.Partial(string,object,PartialOptions)"/> to
        /// request inherited-column indentation.
        /// </summary>
        public static string Partial(string path, object? model = null, PartialOptions? options = null)
        {
            var resolved = ResolvePath(path) ?? throw new FileNotFoundException("File name: '" + path + "'", path);
            if (resolved == null)
            {
                try
                {
                    var fileName = Path.GetFileName(path);
                    var root = Directory.GetCurrentDirectory();
                    var found = Directory.EnumerateFiles(root, fileName, SearchOption.AllDirectories).FirstOrDefault();
                    if (found != null)
                        resolved = found;
                }
                catch { /* ignore search errors */ }
            }
            var engine = new SadRazorEngine.TemplateEngine();
            var ctx = engine.LoadTemplate(resolved!);
            if (model != null)
            {
                ctx = ctx.WithModel(model);
            }
            var result = ctx.RenderAsync().GetAwaiter().GetResult();
            var content = result.Content;

            // If an explicit indent amount was requested, apply it here. We cannot honor
            // InheritColumn from this static helper because we don't have access to the
            // calling template's writer state.
            if (options != null)
            {
                if (options.IndentAmount.HasValue)
                {
                    content = ApplyIndent(content, options.IndentAmount.Value);
                }
                else if (options.InheritColumn)
                {
                    throw new InvalidOperationException(
                        "The 'InheritColumn' option cannot be used with the static TemplateHelpers helper because it requires the compiled template's writer state. " +
                        "Call the instance helper from within a template instead, for example: @(await PartialAsync(\"_partial.md\", options: new PartialOptions { InheritColumn = true })).");
                }
            }

            return content;
        }

        /// <summary>
        /// Asynchronously renders a partial by resolving <paramref name="path"/>. Behaves
        /// identically to <see cref="Partial(string,object,PartialOptions)"/> with respect to
        /// the <see cref="PartialOptions"/> semantics and limitations.
        /// </summary>
        public static async Task<string> PartialAsync(string path, object? model = null, PartialOptions? options = null)
        {
            var resolved = ResolvePath(path) ?? throw new FileNotFoundException("File name: '" + path + "'", path);
            if (resolved == null)
            {
                try
                {
                    var fileName = Path.GetFileName(path);
                    var root = Directory.GetCurrentDirectory();
                    var found = Directory.EnumerateFiles(root, fileName, SearchOption.AllDirectories).FirstOrDefault();
                    if (found != null)
                        resolved = found;
                }
                catch { /* ignore search errors */ }
            }
            var engine = new SadRazorEngine.TemplateEngine();
            var ctx = engine.LoadTemplate(resolved!);
            if (model != null)
            {
                ctx = ctx.WithModel(model);
            }
            var result = await ctx.RenderAsync();
            var content = result.Content;

            if (options != null)
            {
                if (options.IndentAmount.HasValue)
                {
                    content = ApplyIndent(content, options.IndentAmount.Value);
                }
                else if (options.InheritColumn)
                {
                    throw new InvalidOperationException(
                        "The 'InheritColumn' option cannot be used with the static TemplateHelpers helper because it requires the compiled template's writer state. " +
                        "Call the instance helper from within a template instead, for example: @(await PartialAsync(\"_partial.md\", options: new PartialOptions { InheritColumn = true })).");
                }
            }

            return content;
        }

        /// <summary>
        /// Apply explicit indentation to rendered partial content using the shared indentation helper.
        /// </summary>
        private static string ApplyIndent(string content, int indentAmount)
        {
            return IndentationHelper.ApplyIndent(content, indentAmount);
        }
    }
}
