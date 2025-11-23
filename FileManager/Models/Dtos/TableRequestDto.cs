namespace FileManager.Models;

public class TableRequestDto
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public string? Search { get; set; }
    public string? SortColumn { get; set; }
    public int? SortColumnIndex { get; set; }
    public string? SortDirection { get; set; }
}