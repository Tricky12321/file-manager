using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

public sealed class QbittorrentClient : IDisposable
{
    private readonly Uri _baseUri;
    private readonly string _username;
    private readonly string _password;
    private readonly bool _useBasicAuth;
    private readonly HttpClient _http;
    private bool _authenticated;

    public QbittorrentClient(string baseUrl, string username, string password, bool useBasicAuth = true)
    {
        _baseUri = new Uri(baseUrl.TrimEnd('/') + "/");
        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
        _username = username;
        _password = password;

        _http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
        _http.BaseAddress = _baseUri;
        
    }

    /// <summary>For cookie-based auth (qBittorrent default). No-op if using Basic auth.</summary>
    public async Task AuthenticateAsync()
    {
        return;
        if (_useBasicAuth || _authenticated) return;

        using var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("username", _username),
            new KeyValuePair<string, string>("password", _password)
        });

        var resp = await _http.PostAsync("api/v2/auth/login", content).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            throw new HttpRequestException($"Login failed ({(int)resp.StatusCode} {resp.ReasonPhrase}): {body}");
        }
        _authenticated = true;
    }

    /// <summary>Fetch all torrents (filter: "all", "downloading", "completed", etc.).</summary>
    public async Task<IReadOnlyList<TorrentInfo>> GetTorrentsAsync(string filter = "all")
    {
        await AuthenticateAsync().ConfigureAwait(false);
        var url = $"api/v2/torrents/info?filter={WebUtility.UrlEncode(filter)}";
        var resp = await _http.GetAsync(url).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            throw new HttpRequestException($"Failed to fetch torrents ({(int)resp.StatusCode} {resp.ReasonPhrase}): {body}");
        }

        var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
        var torrents = JsonSerializer.Deserialize<List<TorrentInfo>>(json, JsonOptions) ?? new List<TorrentInfo>();
        return torrents;
    }

    /// <summary>Fetch the full file list for a given torrent hash.</summary>
    public async Task<IReadOnlyList<TorrentFile>> GetFilesAsync(string torrentHash)
    {
        await AuthenticateAsync().ConfigureAwait(false);
        var url = $"api/v2/torrents/files?hash={WebUtility.UrlEncode(torrentHash)}";
        var resp = await _http.GetAsync(url).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            throw new HttpRequestException($"Failed to fetch files for {torrentHash} ({(int)resp.StatusCode} {resp.ReasonPhrase}): {body}");
        }

        var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
        var files = JsonSerializer.Deserialize<List<TorrentFile>>(json, JsonOptions) ?? new List<TorrentFile>();
        return files;
    }

    /// <summary>
    /// Convenience: fetch torrents + their files. Uses limited parallelism to avoid hammering the server.
    /// </summary>
    public async Task<IReadOnlyList<TorrentWithFiles>> GetTorrentsWithFilesAsync(string filter = "all", int maxConcurrency = 6, CancellationToken ct = default)
    {
        var torrents = await GetTorrentsAsync(filter).ConfigureAwait(false);
        var gate = new SemaphoreSlim(Math.Max(1, maxConcurrency));
        var tasks = new List<Task<TorrentWithFiles>>(torrents.Count);

        foreach (var t in torrents)
        {
            await gate.WaitAsync(ct).ConfigureAwait(false);
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var files = await GetFilesAsync(t.Hash).ConfigureAwait(false);
                    return new TorrentWithFiles { Torrent = t, Files = files };
                }
                finally
                {
                    gate.Release();
                }
            }, ct));
        }

        return await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public void Dispose() => _http.Dispose();

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };
}

/// <summary>Projection of /api/v2/torrents/info fields you asked for (plus a few helpful ones).</summary>
public sealed class TorrentInfo
{
    [JsonPropertyName("hash")] public string Hash { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";

    // Size in bytes
    [JsonPropertyName("size")] public long SizeBytes { get; set; }

    // Progress is 0..1 (e.g., 0.42 => 42%)
    [JsonPropertyName("progress")] public double Progress { get; set; }

    // Save path (folder)
    [JsonPropertyName("save_path")] public string SavePath { get; set; } = "";

    // Comma-separated tags
    [JsonPropertyName("tags")] public string Tags { get; set; } = "";

    // Category name
    [JsonPropertyName("category")] public string Category { get; set; } = "";

    // Helpful extras
    [JsonPropertyName("state")] public string State { get; set; } = "";
    [JsonPropertyName("dlspeed")] public long DownloadSpeedBytes { get; set; }
    [JsonPropertyName("upspeed")] public long UploadSpeedBytes { get; set; }
    [JsonPropertyName("eta")] public long EtaSeconds { get; set; }
    [JsonPropertyName("content_path")] public string ContentPath { get; set; } = "";
    
    // "total_size" : 9674226784,
    [JsonPropertyName("total_size")] public decimal TotalSizeBytes { get; set; }
    public decimal TotalSizeGigabytes => Math.Round(TotalSizeBytes / (1024 * 1024 * 1024),2);

    // Convenience
    [JsonIgnore] public double Percent => Math.Round(Progress * 100.0, 2);
    [JsonIgnore] public string[] TagList => string.IsNullOrWhiteSpace(Tags)
        ? Array.Empty<string>()
        : Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

/// <summary>Projection of /api/v2/torrents/files rows.</summary>
public sealed class TorrentFile
{
    // File's index within the torrent (useful for other file endpoints)
    [JsonPropertyName("index")] public int Index { get; set; }

    // Path relative to the torrent root
    [JsonPropertyName("name")] public string Name { get; set; } = "";

    // File size in bytes
    [JsonPropertyName("size")] public long SizeBytes { get; set; }

    // 0..1 progress for this file
    [JsonPropertyName("progress")] public double Progress { get; set; }

    // 0..8 priority in qBittorrent
    [JsonPropertyName("priority")] public int Priority { get; set; }

    // Present in newer API versions
    [JsonPropertyName("availability")] public double? Availability { get; set; }

    // Present in some versions; whether the file is fully available for seeding
    [JsonPropertyName("is_seed")] public bool? IsSeed { get; set; }

    // Present in newer versions; start and end piece indices
    [JsonPropertyName("piece_range")] public int[]? PieceRange { get; set; }

    [JsonIgnore] public double Percent => Math.Round(Progress * 100.0, 2);
}

/// <summary>Aggregate object: a torrent and its files.</summary>
public sealed class TorrentWithFiles
{
    public TorrentInfo Torrent { get; set; } = new();
    public IReadOnlyList<TorrentFile> Files { get; set; } = Array.Empty<TorrentFile>();
}
