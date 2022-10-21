using System.Net.Http.Headers;
using System.Reflection;
using LazyAndrew.Enums;
using LazyAndrew.Exceptions;
using Serilog;

namespace LazyAndrew;

public class FileDownloader
{
    private readonly HttpClient _client;
    private readonly string _tempPath;
    private readonly CryptoService _cryptoService;
    
    public FileDownloader()
    {
        Log.Debug("Initializing file downloader");
        _client = new HttpClient();

        var userAgent = new ProductInfoHeaderValue("LazyAndrew",
            Assembly.GetExecutingAssembly().GetName().Version?.ToString());

        _tempPath = Path.Combine(Path.GetTempPath(), "LazyAndrewDownloads");

        if (Directory.Exists(_tempPath) == false)
        {
            Log.Debug("Temp directory doesn't exists, creating directory {Directory}", _tempPath);
            Directory.CreateDirectory(_tempPath);
        }

        _client.DefaultRequestHeaders.UserAgent.Add(userAgent);

        _cryptoService = new CryptoService();
    }

    public async Task<FileInfo> DownloadFile(string url, string? hash, HashAlgorithm hashAlgorithm = HashAlgorithm.sha1)
    {
        Log.Debug("Downloading file from url {Url}", url);
        var stream = await _client.GetByteArrayAsync(url);

        var fi = new FileInfo(Path.Combine(_tempPath, Guid.NewGuid().ToString()));
        Log.Debug("Writing file to temp path {Path}", fi.FullName);
        await File.WriteAllBytesAsync(fi.FullName, stream);
        
        if (!fi.Exists)
        {
            Log.Debug("Download failed, downloaded file not found");
            throw new FileNotFoundException("Download failed");
        }

        // Not checking file hash
        if (hash is null)
        {
            Log.Debug("Has-check not requested");
            return fi;
        }
        Log.Debug("Hash-check requested");
        await using var fileStream = fi.OpenRead();
        
        Log.Debug("Computing hash");
        var downloadHash = _cryptoService.ComputeFileHash(fileStream);
        
        Log.Debug("{Hash} provided hash", hash);
        Log.Debug("{Hash} computed hash", hash);
        if (hash.Equals(downloadHash, StringComparison.CurrentCultureIgnoreCase))
        {
            Log.Debug("Hashes are equal");
            return fi;
        }

        Log.Debug("Hashes are not equal");
        throw new HashNotVerifiedException("Computed hash and provided hash are not equal");
    }
}