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
        /// Synchronous partial render helper. Good for authoring and simple usage.
        /// </summary>
        public static string Partial(string path, object? model = null)
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
            return result.Content;
        }

        /// <summary>
        /// Async partial render helper for templates that can await it.
        /// </summary>
        public static async Task<string> PartialAsync(string path, object? model = null)
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
            return result.Content;
        }
    }
}
