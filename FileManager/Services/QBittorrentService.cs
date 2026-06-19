using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FileManager.Services;

public class QBittorrentService
{
    // Serializes access to the on-disk caches. SemaphoreSlim (unlike lock) lets us await inside the critical section.
    private static readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);

    public QBittorrentService(IConfiguration configuration)
    {
        Configuration = configuration;
        Client = new QbittorrentClient(configuration["qbittorrent:url"], configuration["qbittorrent:username"],
            configuration["qbittorrent:password"]);
        BasePath = configuration["qbittorrent:basePath"];
    }

    public string BasePath { get; set; }

    public QbittorrentClient Client { get; set; }
    public IConfiguration Configuration { get; set; }

    public async Task<List<TorrentInfo>> GetTorrentList(bool clearCache = false)
    {
        await Gate.WaitAsync().ConfigureAwait(false);
        try
        {
            var cachePath = Path.Combine(BasePath, "qbittorrent_cache.json");
            if (clearCache)
            {
                Console.WriteLine("Clearing qBittorrent cache");
                if (File.Exists(cachePath))
                {
                    File.Delete(cachePath);
                }
            }

            if (File.Exists(cachePath))
            {
                Console.WriteLine("Reading qBittorrent cache from disk");
                var cached = JsonConvert.DeserializeObject<List<TorrentInfo>>(await File.ReadAllTextAsync(cachePath).ConfigureAwait(false));
                Console.WriteLine($"Torrents fetched from qBittorrent files: {cached.Count}");
                return cached;
            }

            var torrents = await Client.GetTorrentsAsync().ConfigureAwait(false);
            Console.WriteLine($"Torrents fetched from qBittorrent: {torrents.Count}");
            await File.WriteAllTextAsync(cachePath, JsonConvert.SerializeObject(torrents)).ConfigureAwait(false);
            return torrents.ToList();
        }
        finally
        {
            Gate.Release();
        }
    }

    public async Task<List<string>> GetTorrentFiles(List<TorrentInfo> torrents, bool clearCache = false)
    {
        await Gate.WaitAsync().ConfigureAwait(false);
        try
        {
            var cachePath = Path.Combine(BasePath, "qbittorrent_files.json");
            if (clearCache)
            {
                Console.WriteLine("Clearing qBittorrent files cache");
                if (File.Exists(cachePath))
                {
                    File.Delete(cachePath);
                }
            }

            if (File.Exists(cachePath))
            {
                Console.WriteLine("Reading qBittorrent files cache from disk");
                var cached = JsonConvert.DeserializeObject<List<string>>(await File.ReadAllTextAsync(cachePath).ConfigureAwait(false));
                Console.WriteLine($"Torrents fetched from qBittorrent files: {cached.Count}");
                return cached;
            }

            var output = new List<string>();
            foreach (var torrent in torrents)
            {
                var files = await Client.GetFilesAsync(torrent.Hash).ConfigureAwait(false);
                foreach (var file in files)
                {
                    var fullPath = Path.Combine(torrent.SavePath, file.Name);
                    output.Add(fullPath);
                }
            }
            Console.WriteLine($"Torrent files fetched from qBittorrent: {output.Count}");
            await WriteAllFilesCacheAsync(output).ConfigureAwait(false);
            return output;
        }
        finally
        {
            Gate.Release();
        }
    }

    public async Task UpdateAllFilesCacheAsync(List<string> output)
    {
        await Gate.WaitAsync().ConfigureAwait(false);
        try
        {
            await WriteAllFilesCacheAsync(output).ConfigureAwait(false);
        }
        finally
        {
            Gate.Release();
        }
    }

    // Caller must already hold the gate.
    private Task WriteAllFilesCacheAsync(List<string> output)
    {
        var cachePath = Path.Combine(BasePath, "qbittorrent_files.json");
        return File.WriteAllTextAsync(cachePath, JsonConvert.SerializeObject(output));
    }

    public async Task<List<string>> GetTorrentFilesList(bool clearCache)
    {
        return await GetTorrentFiles(await GetTorrentList(clearCache).ConfigureAwait(false), clearCache).ConfigureAwait(false);
    }
}
