using System.Text;

namespace SadRazorEngine.Runtime
{
    /// <summary>
    /// Small utility helpers used to apply indentation rules to rendered partial content.
    /// </summary>
    public static class IndentationHelper
    {
        /// <summary>
        /// Prefix non-empty lines in <paramref name="content"/> with <paramref name="indentAmount"/> spaces.
        /// Blank lines are preserved (no indentation added) and trailing newline semantics are preserved.
        /// </summary>
        /// <param name="content">The content to indent</param>
        /// <param name="indentAmount">Number of spaces to indent</param>
        /// <param name="skipFirstLine">If true, don't indent the first line (useful when inserting mid-line)</param>
        /// <returns>The indented content</returns>
        public static string ApplyIndent(string content, int indentAmount, bool skipFirstLine = false)
        {
            if (indentAmount <= 0 || string.IsNullOrEmpty(content)) return content;

            var pad = new string(' ', indentAmount);
            var parts = content.Split('\n');
            var sb = new StringBuilder();
            for (int i = 0; i < parts.Length; i++)
            {
                var line = parts[i];
                var isBlank = string.IsNullOrWhiteSpace(line);
                var shouldIndent = !isBlank && !(skipFirstLine && i == 0);
                
                if (shouldIndent)
                    sb.Append(pad).Append(line);
                else
                    sb.Append(line);

                if (i < parts.Length - 1)
                    sb.Append('\n');
            }

            return sb.ToString();
        }
    }
}