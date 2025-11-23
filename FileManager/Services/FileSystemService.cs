using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using FileManager.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Newtonsoft.Json;
using FileInfo = FileManager.Models.FileInfo;

namespace FileManager.Services;

public class FileSystemService
{
    private readonly QBittorrentService _qbittorrentService;

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

    public List<FileInfo> GetFilesInDirectory(string directoryPath, bool? hardlink = null, bool? inQbit = null,
        bool? folderInQbit = null, bool? hashDuplicate = null, bool clearCache = false)
    {
        var qbitFiles = _qbittorrentService.GetTorrentList(clearCache).GetAwaiter().GetResult();
        var qbitAllFiles = _qbittorrentService.GetTorrentFiles(qbitFiles, clearCache).GetAwaiter().GetResult();
        var inodeMap = new Dictionary<(ulong dev, ulong ino), List<(string path, long size)>>();
        int scanned = 0;
        // sha1 hash the directory path for cache
        var directoryHashed = Convert.ToHexString(SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(directoryPath)));
        var cachePath = "/qbit_data/" + directoryHashed + "_file_cache.json";
        if (clearCache)
        {
            if (File.Exists(cachePath))
            {
                File.Delete(cachePath);
            }
        }

        if (File.Exists(cachePath))
        {
            return FilterResults(hardlink, inQbit, folderInQbit, hashDuplicate,
                JsonConvert.DeserializeObject<List<FileInfo>>(File.ReadAllText(cachePath)));
        }

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
        object lockObj = new object();

        Parallel.ForEach(inodeList,
            new ParallelOptions { MaxDegreeOfParallelism = 16 },
            inodeEntry =>
            {
                var firstFile = inodeEntry.files[0];
                string hash = ComputePartialHash(firstFile.path, 1024 * 1024); // 1 MB
                var key = (inodeEntry.dev, inodeEntry.ino);
                inodeHashes[key] = hash;

                lock (lockObj)
                {
                    processedInodes++;
                    if (processedInodes % 10 == 0 || processedInodes == totalInodes)
                    {
                        double pct = processedInodes * 100.0 / totalInodes;
                    }
                }
            });
        // Step 3: Flatten to FileInfo
        var result = new List<FileInfo>();
        foreach (var kv in inodeMap)
        {
            bool isHardlink = kv.Value.Count > 1;
            string inodeId = $"{kv.Key.dev}:{kv.Key.ino}";
            string hash = inodeHashes[kv.Key];

            foreach (var (path, size) in kv.Value)
            {
                result.Add(new FileInfo()
                {
                    Path = path,
                    Inode = inodeId,
                    IsHardlink = isHardlink,
                    Size = size,
                    PartialHash = hash,
                    InQbit = qbitAllFiles.Any(qb => qb == path),
                    FolderInQbit = qbitAllFiles.Any(qb => qb.StartsWith(System.IO.Path.GetDirectoryName(path) ?? "")),
                });
            }
        }
        
        

        Console.WriteLine("Caching file scan results to " + cachePath);
        Console.WriteLine("Total results: " + result.Count);
        Console.WriteLine("Total scanned files: " + scanned);
        Console.WriteLine("Total In Qbittorrent: " + result.Count(f => f.InQbit));
        Console.WriteLine("Total Folder In Qbittorrent: " + result.Count(f => f.FolderInQbit));
        File.WriteAllText(cachePath, JsonConvert.SerializeObject(result));
        result = FilterResults(hardlink, inQbit, folderInQbit, hashDuplicate, result);

        return result;
    }


    private static List<FileInfo> FilterResults(bool? hardlink, bool? inQbit, bool? folderInQbit, bool? hashDuplicate,
        List<FileInfo> result)
    {
        result = result.Select(fi =>
        {
            fi.HashDuplicate = result.Count(f => f.PartialHash == fi.PartialHash) > 1;
            return fi;
        }).ToList();
        return result.Where(file => (hardlink == null || file.IsHardlink == hardlink)
                                    && (inQbit == null || file.InQbit == inQbit)
                                    && (folderInQbit == null || file.FolderInQbit == folderInQbit)
                                    && (hashDuplicate == null || (hashDuplicate == true)
                                        ? file.HashDuplicate
                                        : file.HashDuplicate == false)
                                    ).ToList();
    }

    static string ComputePartialHash(string filePath, int bytesToRead)
    {
        try
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            byte[] buffer = new byte[bytesToRead];
            int read = stream.Read(buffer, 0, bytesToRead);
            byte[] hash = md5.ComputeHash(buffer, 0, read);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
        catch
        {
            return "error";
        }
    }

    public void DeleteFile(string path)
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

        if (File.Exists(path))
        {
            File.Delete(path);
            var qbitAllFiles = _qbittorrentService.GetTorrentFiles(null).GetAwaiter().GetResult();
            var qbitFile = qbitAllFiles.FirstOrDefault(f => f == path);
            if (qbitFile != null)
            {
                qbitAllFiles.Remove(qbitFile);
            }

            _qbittorrentService.UpdateAllFilesCache(qbitAllFiles);
            return;
        }

        throw new FileNotFoundException($"File {path} not found.");
    }

    public void DeleteMultipleFiles(List<string> deleteMultiple)
    {
        try
        {
            foreach (var deleteFile in deleteMultiple)
            {
                DeleteFile(deleteFile);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}