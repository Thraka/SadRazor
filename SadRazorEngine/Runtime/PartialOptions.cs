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
    }
}