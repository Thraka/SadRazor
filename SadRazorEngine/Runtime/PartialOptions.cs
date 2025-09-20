namespace SadRazorEngine.Runtime
{
    /// <summary>
    /// Options that control how partials are rendered with respect to indentation.
    /// </summary>
    public class PartialOptions
    {
        /// <summary>
        /// If set, indent the rendered partial by this many spaces. Wins over InheritColumn when present.
        /// </summary>
        public int? IndentAmount { get; set; }

        /// <summary>
        /// If true, compute the caller's current column and indent to that column. Only supported
        /// when called from a compiled template via `TemplateBase.Partial(...)` or `TemplateBase.PartialAsync(...)`.
        /// </summary>
        public bool InheritColumn { get; set; }

        /// <summary>
        /// If true and InheritColumn is also true, skip indenting the first line of the partial.
        /// This is useful when the partial is being inserted inline after existing content on the same line.
        /// </summary>
        public bool SkipFirstLineIndent { get; set; } = true;
    }
}