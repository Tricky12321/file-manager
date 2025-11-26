namespace SimpleTable.Models;

public class TableResult<T>
{
    /// <summary>
    /// The items in the current page.
    /// </summary>
    public IEnumerable<T> Items { get; set; }
    /// <summary>
    /// The total count of items available (without pagination).
    /// </summary>
    public int TotalCount { get; set; }
    public int ReturnedRecords => Items.Count();
}