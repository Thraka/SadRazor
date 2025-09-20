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
        public static string ApplyIndent(string content, int indentAmount)
        {
            if (indentAmount <= 0 || string.IsNullOrEmpty(content)) return content;

            var pad = new string(' ', indentAmount);
            var parts = content.Split('\n');
            var sb = new StringBuilder();
            for (int i = 0; i < parts.Length; i++)
            {
                var line = parts[i];
                var isBlank = string.IsNullOrWhiteSpace(line);
                if (!isBlank)
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