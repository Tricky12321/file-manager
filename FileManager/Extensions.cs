using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using FileManager.Models;
using FileInfo = System.IO.FileInfo;

namespace FileManager;

public static class Extensions
{
    public static long GetFileSize(this string path)
    {
        return new FileInfo(path).Length;
    }

    public static bool IsNullOrWhitespace(this string path)
    {
        return string.IsNullOrWhiteSpace(path);
    }

    public static void GetPropertyValues(Object obj)
    {
        Type t = obj.GetType();
        Console.WriteLine("Type is: {0}", t.Name);
        PropertyInfo[] props = t.GetProperties();
        Console.WriteLine("Properties (N = {0}):",
            props.Length);
        foreach (var prop in props)
        {
            if (prop.GetIndexParameters().Length == 0)
            {
                Console.WriteLine("   {0} ({1}): {2}", prop.Name, prop.PropertyType.Name, prop.GetValue(obj));
            }

            else
            {
                Console.WriteLine("   {0} ({1}): <Indexed>", prop.Name, prop.PropertyType.Name);
            }
        }
    }
    
    public static TableResult<T> ToTableResponse<T>(this List<T> results, TableRequestDto tableRequest = null)
    {
        var totalResults = results.Count;
        if (tableRequest == null)
        {
            tableRequest = new TableRequestDto()
            {
                PageNumber = 1,
                PageSize = totalResults,
                Search = string.Empty,
                SortColumn = null,
                SortDirection = "asc"
            };
        }
        // Apply search filter
        if (!string.IsNullOrWhiteSpace(tableRequest.Search))
        {
            // Search all fields that are strings or can be cast to string, should use reflection to do this
            results = results.Where(r =>
            {
                var properties = r.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    if (prop.PropertyType == typeof(string) || prop.PropertyType.IsValueType)
                    {
                        var value = prop.GetValue(r)?.ToString();
                        if (value != null && value.Contains(tableRequest.Search, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }).ToList();
        }


        // Apply sorting
        if (tableRequest.SortColumn != null)
        {
            //Extensions.GetPropertyValues(results.First());
            results = tableRequest.SortDirection == "desc"
                ? results.OrderByDescending(r =>
                {
                    return r.GetType().GetProperty(tableRequest.SortColumn)?.GetValue(r);
                }).ToList()
                : results.OrderBy(r =>
                {
                    return r.GetType().GetProperty(tableRequest.SortColumn)?.GetValue(r);
                }).ToList();
        }
        // Apply pagination
        results = results.Skip((tableRequest.PageNumber - 1) * tableRequest.PageSize)
            .Take(tableRequest.PageSize)
            .ToList();
        var output = new TableResult<T>()
        {
            Items = results,
            TotalCount = totalResults,
        };
        return output;
    }

    public static string Sha1Hash(this string input)
    {
        if (input == null)
        {
            return null;
        }
        return Convert.ToHexString(SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(input)));
    }
    
    public static List<Models.FileInfo> FilterResults(this List<Models.FileInfo> result, string directoryPath, bool? hardlink, bool? inQbit, bool? folderInQbit, bool? hashDuplicate)
    {
        var patialHashDictionary = result.GroupBy(x => x.PartialHash).ToDictionary(x => x.Key, x => x.Count());
        result = result.Select(fi =>
        {
            fi.HashDuplicate = patialHashDictionary[fi.PartialHash] > 1;
            return fi;
        }).ToList();
        return result
            .Where(x => x.FolderPath != null && x.FolderPath.StartsWith(directoryPath))
            .Where(file => (hardlink == null || file.IsHardlink == hardlink)
                                    && (inQbit == null || file.InQbit == inQbit)
                                    && (folderInQbit == null || file.FolderInQbit == folderInQbit)
                                    && (hashDuplicate == null || (hashDuplicate == true
                                        ? file.HashDuplicate
                                        : file.HashDuplicate == false))
        ).ToList();
    }

}