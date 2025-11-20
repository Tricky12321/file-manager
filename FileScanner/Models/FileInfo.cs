using System;

namespace FileManager.Models;

public class FileInfo
{
    public bool IsHardlink { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Inode { get; set; } = string.Empty;
    public long Size { get; set; }
    public decimal SizeMb => Math.Round(Size / 1024m / 1024m, 2);
    public decimal SizeGb => Math.Round(Size / 1024m / 1024m / 1024m, 2);

    public string PartialHash { get; set; } = string.Empty;

    // Return the folder of the path to the file
    public string? FolderPath => System.IO.Path.GetDirectoryName(Path);
    public string FolderName => System.IO.Path.GetFileName(FolderPath);

    public bool InQbit { get; set; }
    public bool FolderInQbit { get; set; }
    
}