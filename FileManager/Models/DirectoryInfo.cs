using System;

namespace FileManager.Models;

public class DirectoryInfo
{
    public string Path { get; set; }
    public long Size { get; set; }
    public decimal SizeMb => Math.Round(Size / 1024m / 1024m, 2);
    public decimal SizeGb => Math.Round(Size / 1024m / 1024m / 1024m, 2);

    public bool FolderInQbit { get; set; }
    public int FileCount { get; set; }
    public bool Selected = false;
}