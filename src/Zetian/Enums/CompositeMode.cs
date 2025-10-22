namespace Zetian.Enums
{
    /// <summary>
    /// Composite filter mode
    /// </summary>
    public enum CompositeMode
    {
        /// <summary>
        /// All filters must accept (AND logic)
        /// </summary>
        All,

        /// <summary>
        /// At least one filter must accept (OR logic)
        /// </summary>
        Any
    }
}