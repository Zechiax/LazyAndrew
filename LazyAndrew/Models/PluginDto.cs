using Modrinth.RestClient.Models;
using Version = Modrinth.RestClient.Models.Version;

namespace LazyAndrew.Models;

public class PluginDto
{
    public bool OnModrinth = true;
    public FileInfo? File { get; set; }
    public bool UpToDate { get; set; } = false;
    public Version? LatestVersion { get; set; }
    public Version? CurrentVersion { get; set; }
    public Project? Project { get; set; }
}