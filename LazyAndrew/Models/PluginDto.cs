using Modrinth.RestClient.Models;
using Version = Modrinth.RestClient.Models.Version;

namespace LazyAndrew.Models;

public class PluginDto
{
    public FileInfo File { get; }
    public Version? LatestVersion { get; set; }
    public Version CurrentVersion { get; }
    public Project? Project { get; set; }

    public PluginDto(Version currentversion, FileInfo file)
    {
        CurrentVersion = currentversion;
        File = file;
    }
}