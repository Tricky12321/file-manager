using System;
using System.Collections.Generic;
using System.IO;
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
        var patialHashDictionary = result.Where(x => !x.PartialHash.IsNullOrWhitespace()).GroupBy(x => x.PartialHash).ToDictionary(x => x.Key, x => x.Count());
        result = result.Select(fi =>
        {
            if (fi.PartialHash != null && patialHashDictionary.TryGetValue(fi.PartialHash, out var value))
            {
                fi.HashDuplicate = value > 1;
            }
            else
            {
                fi.HashDuplicate = false;
            }

            return fi;
        }).ToList();
        // Match the requested root exactly (with a trailing separator) so that e.g.
        // "/torrent/TV" does not also capture files under "/torrent/TV-link".
        var rootPrefix = directoryPath.TrimEnd('/') + "/";
        return result
            .Where(x => x.Path != null && x.Path.StartsWith(rootPrefix))
            .Where(file => (hardlink == null || file.IsHardlink == hardlink)
                           && (inQbit == null || file.InQbit == inQbit)
                           && (folderInQbit == null || file.FolderInQbit == folderInQbit)
                           && (hashDuplicate == null || (hashDuplicate == true
                               ? file.HashDuplicate
                               : file.HashDuplicate == false))
            ).ToList();
    }

    public static bool IsDirectory(this string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                FileAttributes attr = File.GetAttributes(path);
                return attr.HasFlag(FileAttributes.Directory);    
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        return false;
    }
}