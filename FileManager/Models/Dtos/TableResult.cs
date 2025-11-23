using System.Collections.Generic;

namespace FileManager.Models;

public class TableResult<T>
{
    /*
     * export interface TableResult<T> {
         items: T[];
         totalCount: number;
       }
     */
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
}