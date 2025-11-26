using System.Collections.Generic;

namespace FileManager.Models;

public class TableResult<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
}