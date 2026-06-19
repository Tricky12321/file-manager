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
    public bool HashDuplicate { get; set; }
    public bool IsSample { get; set; }
    // True when this file's inode also exists under the paired folder (main <-> link),
    // i.e. the file is hardlinked into both the main library and its -link folder.
    public bool InBoth { get; set; }
    public string TorrentPath { get; set; }

    public bool Selected { get; set; }
}