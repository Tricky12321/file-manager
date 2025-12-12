using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using FileManager.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Newtonsoft.Json;
using DirectoryInfo = FileManager.Models.DirectoryInfo;
using FileInfo = FileManager.Models.FileInfo;

namespace FileManager.Services;

public class FileSystemService
{
    private readonly QBittorrentService _qbittorrentService;
    private static object _lockObj = new object();

    private string[] ScanFolders = new string[]
    {
        "/torrent/TV",
        "/torrent/Film",
    };

    public FileSystemService(QBittorrentService qBittorrentService)
    {
        _qbittorrentService = qBittorrentService;
    }

    [DllImport("libc", SetLastError = true)]
    private static extern int stat(string path, out Stat buf);

    [StructLayout(LayoutKind.Sequential)]
    struct Stat
    {
        public ulong st_dev;
        public ulong st_ino;
        public ulong st_nlink;
        public uint st_mode;
        public uint st_uid;
        public uint st_gid;
        public ulong __pad0;
        public ulong st_rdev;
        public long st_size;
        public long st_blksize;
        public long st_blocks;
        public long st_atime;
        public ulong st_atime_nsec;
        public long st_mtime;
        public ulong st_mtime_nsec;
        public long st_ctime;
        public ulong st_ctime_nsec;
        public long __unused4;
        public long __unused5;
    }

    public List<FileInfo> GetFilesInDirectories(List<string> directories)
    {
        var output = new List<FileInfo>();
        foreach (var dir in directories)
        {
            var files = GetFilesInDirectory(dir);
            output.AddRange(files);
        }

        return output;
    }

    public List<FileInfo> GetDirectoriesInDirectory(string directoryPath, bool? folderInQbit = null, bool clearCache = false)
    {
        var qbittorrentFiles = _qbittorrentService.GetTorrentList(clearCache).GetAwaiter().GetResult();
        var fileInfos = new List<FileInfo>();
        foreach (var dir in Directory.EnumerateDirectories(directoryPath, "*", SearchOption.AllDirectories))
        {
            if (dir == directoryPath)
            {
                continue;
            }

            long totalSize = 0;
            foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
            {
                try
                {
                    var fileInfo = new System.IO.FileInfo(file);
                    totalSize += fileInfo.Length;
                }
                catch
                {
                    // Ignore unreadable files
                }
            }

            fileInfos.Add(new FileInfo()
            {
                Path = dir,
                Size = totalSize,
                IsHardlink = false,
                HashDuplicate = false,
                FolderInQbit = qbittorrentFiles.Any(x => x.ContentPath.StartsWith(dir))
            });
        }

        return fileInfos;
        /*

        var fileInfos = new List<FileInfo>();
        var qbittorrentFiles = _qbittorrentService.GetTorrentList(clearCache).GetAwaiter().GetResult();
        Console.WriteLine("Scanning for folders in " + directoryPath);
        var folders = Directory.EnumerateDirectories(directoryPath, "*", SearchOption.AllDirectories);
        Console.WriteLine("Found " + folders.Count() + " folders");
        foreach (var dir in Directory.EnumerateDirectories(directoryPath, "*", SearchOption.AllDirectories))
        {
            fileInfos.Add(new FileInfo()
            {
                Path = dir,
                Size = dir.Length,
                FolderInQbit = qbittorrentFiles.Any(x => x.ContentPath.StartsWith(dir))
            });
        }

        Console.WriteLine("Folder scan complete, found " + fileInfos.Count + " folders");
        return fileInfos;
        List<FileInfo> files = GetFilesInDirectory(directoryPath, null, null, folderInQbit, null, clearCache);
        var grouped = files.GroupBy(f => f.FolderPath);
        var folders = new List<DirectoryInfo>();
        foreach (var group in grouped)
        {
            folders.Add(new DirectoryInfo()
            {
                Path = group.Key,
                FileCount = group.Count(),
                FolderInQbit = group.Any(f => f.FolderInQbit),
                Size = group.Sum(f => f.Size)
            });
        }
        return folders;
        */
    }

    public List<FileInfo> GetFilesInDirectory(string directoryPath, bool? hardlink = null, bool? inQbit = null, bool? folderInQbit = null, bool? hashDuplicate = null, bool clearCache = false)
    {
        var qbitFiles = _qbittorrentService.GetTorrentList(clearCache).GetAwaiter().GetResult();
        var qbitAllFiles = _qbittorrentService.GetTorrentFiles(qbitFiles, clearCache).GetAwaiter().GetResult();
        var inodeMap = new Dictionary<(ulong dev, ulong ino), List<(string path, long size)>>();
        int scanned = 0;

        lock (_lockObj)
        {
            // sha1 hash the directory path for cache
            var cachePath = "/qbit_data/file_cache.json";
            if (clearCache)
            {
                if (File.Exists(cachePath))
                {
                    File.Delete(cachePath);
                }
            }

            if (File.Exists(cachePath))
            {
                return JsonConvert.DeserializeObject<List<FileInfo>>(File.ReadAllText(cachePath))
                    .FilterResults(directoryPath, hardlink, inQbit, folderInQbit, hashDuplicate);
            }

            List<FileInfo> result = new List<FileInfo>();
            foreach (var folder in ScanFolders)
            {
                result.AddRange(ScanFilesInPath(folder, inodeMap, qbitAllFiles, ref scanned));
            }


            Console.WriteLine("Caching file scan results to " + cachePath);
            Console.WriteLine("Total results: " + result.Count);
            Console.WriteLine("Total scanned files: " + scanned);
            Console.WriteLine("Total In Qbittorrent: " + result.Count(f => f.InQbit));
            Console.WriteLine("Total Folder In Qbittorrent: " + result.Count(f => f.FolderInQbit));
            File.WriteAllText(cachePath, JsonConvert.SerializeObject(result));
            result = result.FilterResults(directoryPath, hardlink, inQbit, folderInQbit, hashDuplicate);
            return result;
        }
    }

    private static List<FileInfo> ScanFilesInPath(string directoryPath, Dictionary<(ulong dev, ulong ino), List<(string path, long size)>> inodeMap, List<string> qbitAllFiles, ref int scanned,
        bool hashCheck = false)
    {
        // Step 1: Scan all files and collect size + inode
        foreach (var file in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
        {
            try
            {
                if (stat(file, out Stat sb) == 0)
                {
                    var key = (sb.st_dev, sb.st_ino);
                    if (!inodeMap.ContainsKey(key))
                    {
                        inodeMap[key] = new List<(string, long)>();
                    }

                    // Get the disk size of the file
                    var diskSize = file.GetFileSize();
                    inodeMap[key].Add((file, diskSize));
                    scanned++;
                    if (scanned % 1000 == 0) Console.Write(".");
                }
            }
            catch
            {
                Console.WriteLine("Unable to read file: " + file);
                // skip unreadable files
            }
        }

        // Step 2: Compute partial hash in parallel
        var inodeHashes = new ConcurrentDictionary<(ulong dev, ulong ino), string>();
        var inodeList = new List<(ulong dev, ulong ino, List<(string path, long size)> files)>();
        foreach (var kv in inodeMap)
        {
            inodeList.Add((kv.Key.dev, kv.Key.ino, kv.Value));
        }

        int totalInodes = inodeList.Count;
        int processedInodes = 0;
        var lockObj = new object();
        if (hashCheck)
        {
            Parallel.ForEach(inodeList, inodeEntry =>
            {
                var firstFile = inodeEntry.files[0];
                string hash = ComputePartialHash(firstFile.path, 1024 * 1024 * 8); // 8 MB
                var key = (inodeEntry.dev, inodeEntry.ino);
                inodeHashes[key] = hash;

                lock (lockObj)
                {
                    processedInodes++;
                    if (processedInodes % 50 == 0 || processedInodes == totalInodes)
                    {
                        double pct = processedInodes * 100.0 / totalInodes;
                        Console.WriteLine($"Computed hashes for {processedInodes}/{totalInodes} inodes ({pct:F2}%)");
                    }
                }
            });
        }

        // Step 3: Flatten to FileInfo
        var result = new List<FileInfo>();
        foreach (var kv in inodeMap)
        {
            bool isHardlink = kv.Value.Count > 1;
            string inodeId = $"{kv.Key.dev}:{kv.Key.ino}";

            foreach (var (path, size) in kv.Value)
            {
                result.Add(new FileInfo()
                {
                    Path = path,
                    Inode = inodeId,
                    IsHardlink = isHardlink,
                    Size = size,
                    PartialHash = hashCheck ? inodeHashes[kv.Key] : string.Empty,
                    InQbit = qbitAllFiles.Any(qb => qb == path),
                    FolderInQbit =
                        qbitAllFiles.Any(qb => qb.StartsWith(System.IO.Path.GetDirectoryName(path) ?? "")),
                });
            }
        }

        return result;
    }

    static string ComputePartialHash(string filePath, int bytesToRead)
    {
        try
        {
            using var sha1 = SHA1.Create();
            using var stream = File.OpenRead(filePath);
            byte[] buffer = new byte[bytesToRead];
            int read = stream.Read(buffer, 0, bytesToRead);
            byte[] hash = sha1.ComputeHash(buffer, 0, read);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
        catch
        {
            return "error";
        }
    }

    public void DeleteFile(string path, string directoryPath)
    {
        if (path.IsNullOrWhitespace())
        {
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));
        }

        // Check if the path is less than 10 characters to avoid accidental deletions
        if (path.Length < 10)
        {
            throw new ArgumentException("Path is too short, deletion aborted for safety.", nameof(path));
        }

        Console.WriteLine($"Deleting file {path}");
        if (File.Exists(path))
        {
            Console.WriteLine($"Deleting file {path}");
            File.Delete(path);
            Console.WriteLine($"Deleted file {path}");
            RemoveFileFromCache(path, directoryPath);
            return;
        }
        else
        {
            Console.WriteLine("File not found: " + path);
            RemoveFileFromCache(path, directoryPath);
        }

        throw new FileNotFoundException($"File {path} not found.");
    }

    public void DeleteFolder(string folderPath)
    {
        if (folderPath.IsNullOrWhitespace())
        {
            throw new ArgumentException("Folder path cannot be null or empty.", nameof(folderPath));
        }

        // Check if the path is less than 10 characters to avoid accidental deletions
        if (folderPath.Length < 10)
        {
            throw new ArgumentException("Folder path is too short, deletion aborted for safety.", nameof(folderPath));
        }

        Console.WriteLine($"Deleting folder {folderPath}");
        if (Directory.Exists(folderPath))
        {
            Console.WriteLine($"Deleting folder {folderPath}");
            Directory.Delete(folderPath, true);
            RemoveFolderFromCache(folderPath);
            Console.WriteLine($"Deleted folder {folderPath}");
            return;
        }
        else
        {
            Console.WriteLine("Folder not found: " + folderPath);
        }

        throw new DirectoryNotFoundException($"Folder {folderPath} not found.");
    }

    public List<FileInfo> GetEmptyFolders(string rootPath)
    {
        var fileInfos = new List<FileInfo>();
        foreach (var dir in Directory.EnumerateDirectories(rootPath, "*", SearchOption.AllDirectories))
        {
            if (!Directory.EnumerateFileSystemEntries(dir).Any())
            {
                if (dir == rootPath)
                {
                    continue;
                }

                fileInfos.Add(new FileInfo()
                {
                    Path = dir,
                    Size = dir.Length,
                });
            }
        }

        return fileInfos;
    }

    public List<FileInfo> GetSmallFolders(string rootPath, long sizeThresholdBytes = 10485760) // Default 10 MB
    {
        var fileInfos = new List<FileInfo>();
        foreach (var dir in Directory.EnumerateDirectories(rootPath, "*", SearchOption.AllDirectories))
        {
            if (dir == rootPath)
            {
                continue;
            }

            long totalSize = 0;
            foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
            {
                try
                {
                    var fileInfo = new System.IO.FileInfo(file);
                    totalSize += fileInfo.Length;
                }
                catch
                {
                    // Ignore unreadable files
                }
            }

            if (totalSize < sizeThresholdBytes)
            {
                fileInfos.Add(new FileInfo()
                {
                    Path = dir,
                    Size = totalSize,
                    IsHardlink = false,
                    HashDuplicate = false,
                    FolderInQbit = false,
                });
            }
        }

        return fileInfos;
    }

    private void RemoveFileFromCache(string path, string directoryPath)
    {
        var cachePath = "/qbit_data/file_cache.json";
        var data = JsonConvert.DeserializeObject<List<FileInfo>>(File.ReadAllText(cachePath));
        // Remove from cache
        data = data.Where(f => f.Path != path).ToList();
        File.WriteAllText(cachePath, JsonConvert.SerializeObject(data));
        var qbitAllFiles = _qbittorrentService.GetTorrentFiles(null).GetAwaiter().GetResult();
        var qbitFile = qbitAllFiles.FirstOrDefault(f => f == path);
        if (qbitFile != null)
        {
            qbitAllFiles.Remove(qbitFile);
        }

        // Update qBittorrent cache
        Console.WriteLine($"Updating qBittorrent cache after deleting file {path}");
        _qbittorrentService.UpdateAllFilesCache(qbitAllFiles);
    }

    private void RemoveFolderFromCache(string directoryPath)
    {
        var cachePath = "/qbit_data/file_cache.json";
        var data = JsonConvert.DeserializeObject<List<FileInfo>>(File.ReadAllText(cachePath));
        // Remove from cache
        data = data.Where(f => !f.Path.StartsWith(directoryPath)).ToList();
        File.WriteAllText(cachePath, JsonConvert.SerializeObject(data));
        var qbitAllFiles = _qbittorrentService.GetTorrentFiles(null).GetAwaiter().GetResult();
        var qbitFile = qbitAllFiles.Where(f => f.StartsWith(directoryPath)).ToList();
        if (qbitFile.Any())
        {
            foreach (var file in qbitFile)
            {
                qbitAllFiles.Remove(file);
            }
        }

        // Update qBittorrent cache
        Console.WriteLine($"Updating qBittorrent cache after deleting folder {directoryPath}");
        _qbittorrentService.UpdateAllFilesCache(qbitAllFiles);
    }

    public void DeleteMultipleFiles(List<string> deleteMultiple, string folderPath)
    {
        try
        {
            foreach (var deleteFile in deleteMultiple)
            {
                try
                {
                    DeleteFile(deleteFile, folderPath);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}