namespace SimpleTable.Models;

public class TableRequest
{
    /// <summary>
    /// The rquested page number.
    /// </summary>
    public int PageNumber { get; set; }
    /// <summary>
    /// The requested page size.
    /// </summary>
    public int PageSize { get; set; }
    /// <summary>
    /// The search term. null if no search is applied.
    /// </summary>
    public string? Search { get; set; }
    /// <summary>
    /// The name of the column to sort by. null if no sorting is applied.
    /// </summary>
    public string? SortColumn { get; set; }
    /// <summary>
    /// The index of the column to sort by. null if no sorting is applied.
    /// </summary>
    public int? SortColumnIndex { get; set; }
    /// <summary>
    /// The direction of the sort. "asc" or "desc". null if no sorting is applied.
    /// </summary>
    public string? SortDirection { get; set; }
}