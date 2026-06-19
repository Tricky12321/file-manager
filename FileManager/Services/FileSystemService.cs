using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
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
    // Serializes the file-scan/cache critical section. SemaphoreSlim allows awaiting inside it.
    private static readonly SemaphoreSlim _scanGate = new SemaphoreSlim(1, 1);

    private const string FileCacheFile = "/qbit_data/file_cache.json";
    private const string HashCacheFile = "/qbit_data/file_cache_hash.json";

    private string[] ScanFolders = new string[]
    {
        "/torrent/TV",
        "/torrent/Film",
    };

    // Each main library folder paired with its hardlink folder. A file is "in both" when
    // the same inode appears under both sides of a pair.
    private static readonly (string main, string link)[] LinkPairs = new[]
    {
        ("/torrent/TV", "/torrent/TV-link"),
        ("/torrent/Film", "/torrent/Film-link"),
    };

    // All roots scanned into the shared inode map (main folders + their link folders).
    private string[] AllScanFolders => ScanFolders.Concat(LinkPairs.Select(p => p.link)).ToArray();

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

    public async Task<List<FileInfo>> GetDirectoriesInDirectory(string directoryPath, bool? folderInQbit = null, bool clearCache = false)
    {
        var qbittorrentFiles = await _qbittorrentService.GetTorrentList(clearCache).ConfigureAwait(false);
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

            var torrentPath = "";
            var inQbit = qbittorrentFiles.Any(x =>
            {
                var directoryName = System.IO.Path.GetDirectoryName(x.ContentPath);

                if (dir.StartsWith(x.ContentPath) || (!ScanFolders.Contains(directoryName) && (directoryName.StartsWith(dir) || dir.StartsWith(directoryName))))
                {
                    torrentPath = x.ContentPath;
                    return true;
                }

                return false;
            });

            fileInfos.Add(new FileInfo()
            {
                Path = dir,
                Size = totalSize,
                IsHardlink = false,
                HashDuplicate = false,
                TorrentPath = torrentPath,
                FolderInQbit = inQbit
            });
        }

        if (folderInQbit == true)
        {
            fileInfos = fileInfos.Where(f => f.FolderInQbit).ToList();
        }
        else if (folderInQbit == false)
        {
            fileInfos = fileInfos.Where(f => !f.FolderInQbit).ToList();
        }

        return fileInfos;
    }

    public async Task<List<FileInfo>> GetFilesInDirectory(string directoryPath, bool? hardlink = null, bool? inQbit = null, bool? folderInQbit = null, bool? hashDuplicate = null, bool clearCache = false)
    {
        var qbitFiles = await _qbittorrentService.GetTorrentList(clearCache).ConfigureAwait(false);
        var qbitAllFiles = await _qbittorrentService.GetTorrentFiles(qbitFiles, clearCache).ConfigureAwait(false);
        var inodeMap = new Dictionary<(ulong dev, ulong ino), List<(string path, long size)>>();
        int scanned = 0;

        await _scanGate.WaitAsync().ConfigureAwait(false);
        try
        {
            // Hashing is expensive (8 MB read per inode), so only compute partial hashes
            // when the caller actually wants to filter on duplicates. Hashed and non-hashed
            // scans are cached separately so the two result sets never overwrite each other.
            var hashCheck = hashDuplicate != null;
            var cachePath = hashCheck ? HashCacheFile : FileCacheFile;

            // A cache clear should invalidate both variants so a refresh is consistent.
            if (clearCache)
            {
                foreach (var cache in new[] { FileCacheFile, HashCacheFile })
                {
                    if (File.Exists(cache))
                    {
                        File.Delete(cache);
                    }
                }
            }

            if (File.Exists(cachePath))
            {
                return JsonConvert.DeserializeObject<List<FileInfo>>(File.ReadAllText(cachePath))
                    .FilterResults(directoryPath, hardlink, inQbit, folderInQbit, hashDuplicate);
            }

            // Collect inodes across all roots first, then flatten once. A single shared map
            // lets us detect hardlinks across roots and the main<->link pairing.
            foreach (var folder in AllScanFolders)
            {
                CollectInodes(folder, inodeMap, ref scanned);
            }

            List<FileInfo> result = FlattenInodes(inodeMap, qbitAllFiles, hashCheck);

            Console.WriteLine("Caching file scan results to " + cachePath);
            Console.WriteLine("Total results: " + result.Count);
            Console.WriteLine("Total scanned files: " + scanned);
            Console.WriteLine("Total In Qbittorrent: " + result.Count(f => f.InQbit));
            Console.WriteLine("Total Folder In Qbittorrent: " + result.Count(f => f.FolderInQbit));
            File.WriteAllText(cachePath, JsonConvert.SerializeObject(result));
            result = result.FilterResults(directoryPath, hardlink, inQbit, folderInQbit, hashDuplicate);
            return result;
        }
        finally
        {
            _scanGate.Release();
        }
    }

    // Walk a root and record each file's (dev, ino) + on-disk size into the shared map.
    private static void CollectInodes(string directoryPath, Dictionary<(ulong dev, ulong ino), List<(string path, long size)>> inodeMap, ref int scanned)
    {
        if (!Directory.Exists(directoryPath))
        {
            return;
        }

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
    }

    // Flatten the inode map to FileInfo, computing hardlink, hash, qbit and main<->link pairing.
    private static List<FileInfo> FlattenInodes(Dictionary<(ulong dev, ulong ino), List<(string path, long size)>> inodeMap, List<string> qbitAllFiles, bool hashCheck)
    {
        var inodeHashes = new ConcurrentDictionary<(ulong dev, ulong ino), string>();
        if (hashCheck)
        {
            var inodeList = inodeMap.Select(kv => (kv.Key, kv.Value)).ToList();
            int totalInodes = inodeList.Count;
            int processedInodes = 0;
            var lockObj = new object();
            Parallel.ForEach(inodeList, inodeEntry =>
            {
                var firstFile = inodeEntry.Value[0];
                inodeHashes[inodeEntry.Key] = ComputePartialHash(firstFile.path, 1024 * 1024 * 8); // 8 MB

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

        var result = new List<FileInfo>();
        foreach (var kv in inodeMap)
        {
            bool isHardlink = kv.Value.Count > 1;
            string inodeId = $"{kv.Key.dev}:{kv.Key.ino}";

            // An inode is "in both" when, for some pair, it has a path under the main folder
            // and a path under that pair's link folder.
            bool inBoth = LinkPairs.Any(pair =>
                kv.Value.Any(f => f.path.StartsWith(pair.main + "/")) &&
                kv.Value.Any(f => f.path.StartsWith(pair.link + "/")));

            foreach (var (path, size) in kv.Value)
            {
                result.Add(new FileInfo()
                {
                    Path = path,
                    Inode = inodeId,
                    IsHardlink = isHardlink,
                    Size = size,
                    PartialHash = hashCheck ? inodeHashes[kv.Key] : string.Empty,
                    InBoth = inBoth,
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

    public async Task DeleteFile(string path, string? directoryPath)
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
            await RemoveFileFromCache(path, directoryPath).ConfigureAwait(false);
            return;
        }
        else
        {
            Console.WriteLine("File not found: " + path);
            await RemoveFileFromCache(path, directoryPath).ConfigureAwait(false);
        }

        throw new FileNotFoundException($"File {path} not found.");
    }

    public async Task DeleteFolder(string folderPath)
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
            await RemoveFolderFromCache(folderPath).ConfigureAwait(false);
            Console.WriteLine($"Deleted folder {folderPath}");
            return;
        }
        else
        {
            Console.WriteLine("Folder not found: " + folderPath);
        }

        //throw new DirectoryNotFoundException($"Folder {folderPath} not found.");
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

    // Scans a (link) folder for "sample" files: files whose name contains "sample",
    // or that live inside a folder named "Sample"/"Samples". Returned pre-selected so the
    // UI marks them for deletion; the user can deselect false positives before deleting.
    public List<FileInfo> GetSampleFiles(string rootPath)
    {
        var fileInfos = new List<FileInfo>();
        foreach (var file in Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories))
        {
            if (!IsSampleFile(file))
            {
                continue;
            }

            long size = 0;
            try
            {
                size = new System.IO.FileInfo(file).Length;
            }
            catch
            {
                // Ignore unreadable files
            }

            fileInfos.Add(new FileInfo()
            {
                Path = file,
                Size = size,
                IsSample = true,
                Selected = true,
            });
        }

        return fileInfos;
    }

    private static bool IsSampleFile(string filePath)
    {
        var fileName = System.IO.Path.GetFileName(filePath);
        if (fileName.Contains("sample", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var directory = System.IO.Path.GetDirectoryName(filePath) ?? string.Empty;
        foreach (var segment in directory.Split(System.IO.Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment.Equals("sample", StringComparison.OrdinalIgnoreCase) || segment.Equals("samples", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private async Task RemoveFileFromCache(string path, string? directoryPath)
    {
        foreach (var cachePath in new[] { FileCacheFile, HashCacheFile })
        {
            if (!File.Exists(cachePath))
            {
                continue;
            }

            var cached = JsonConvert.DeserializeObject<List<FileInfo>>(await File.ReadAllTextAsync(cachePath).ConfigureAwait(false))
                .Where(f => f.Path != path).ToList();
            await File.WriteAllTextAsync(cachePath, JsonConvert.SerializeObject(cached)).ConfigureAwait(false);
        }

        var qbitAllFiles = await _qbittorrentService.GetTorrentFiles(null).ConfigureAwait(false);
        var qbitFile = qbitAllFiles.FirstOrDefault(f => f == path);
        if (qbitFile != null)
        {
            qbitAllFiles.Remove(qbitFile);
        }

        // Update qBittorrent cache
        Console.WriteLine($"Updating qBittorrent cache after deleting file {path}");
        await _qbittorrentService.UpdateAllFilesCacheAsync(qbitAllFiles).ConfigureAwait(false);
    }

    private async Task RemoveFolderFromCache(string directoryPath)
    {
        foreach (var cachePath in new[] { FileCacheFile, HashCacheFile })
        {
            if (!File.Exists(cachePath))
            {
                continue;
            }

            var cached = JsonConvert.DeserializeObject<List<FileInfo>>(await File.ReadAllTextAsync(cachePath).ConfigureAwait(false))
                .Where(f => !f.Path.StartsWith(directoryPath)).ToList();
            await File.WriteAllTextAsync(cachePath, JsonConvert.SerializeObject(cached)).ConfigureAwait(false);
        }

        var qbitAllFiles = await _qbittorrentService.GetTorrentFiles(null).ConfigureAwait(false);
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
        await _qbittorrentService.UpdateAllFilesCacheAsync(qbitAllFiles).ConfigureAwait(false);
    }

    public async Task DeleteMultipleFiles(List<string> deleteMultiple, string? folderPath)
    {
        foreach (var deleteFile in deleteMultiple)
        {
            try
            {
                await DeleteFile(deleteFile, folderPath).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}