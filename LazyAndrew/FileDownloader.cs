using System.Net.Http.Headers;
using System.Reflection;
using LazyAndrew.Enums;
using LazyAndrew.Exceptions;

namespace LazyAndrew;

public class FileDownloader
{
    private readonly HttpClient _client;
    private readonly string _tempPath;
    private readonly CryptoService _cryptoService;
    
    public FileDownloader()
    {
        _client = new HttpClient();

        var userAgent = new ProductInfoHeaderValue("LazyAndrew",
            Assembly.GetExecutingAssembly().GetName().Version?.ToString());

        _tempPath = Path.Combine(Path.GetTempPath(), "LazyAndrewDownloads");

        if (Directory.Exists(_tempPath) == false)
        {
            Directory.CreateDirectory(_tempPath);
        }

        _client.DefaultRequestHeaders.UserAgent.Add(userAgent);

        _cryptoService = new CryptoService();
    }

    public async Task<FileInfo> DownloadFile(string url, string? hash, HashAlgorithm hashAlgorithm = HashAlgorithm.sha1)
    {
        var stream = await _client.GetByteArrayAsync(url);

        var fi = new FileInfo(Path.Combine(_tempPath, Guid.NewGuid().ToString()));
        await File.WriteAllBytesAsync(fi.FullName, stream);

        if (!fi.Exists)
        {
            throw new FileNotFoundException("Download failed");
        }

        // Not checking file hash
        if (hash is null)
        {
            return fi;
        }

        await using var fileStream = fi.OpenRead();
        
        var downloadHash = _cryptoService.ComputeFileHash(fileStream);
        
        if (hash.Equals(downloadHash, StringComparison.CurrentCultureIgnoreCase))
        {
            return fi;
        }

        throw new HashNotVerified("Hash string are not equal");
    }
}