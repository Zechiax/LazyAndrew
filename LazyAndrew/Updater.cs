using System.Net;
using System.Security.Cryptography;
using Modrinth.RestClient;
using Modrinth.RestClient.Extensions;
using RestEase;
using ShellProgressBar;
using HashAlgorithm = Modrinth.RestClient.Models.Enums.HashAlgorithm;
using Version = Modrinth.RestClient.Models.Version;

namespace LazyAndrew;

public class Updater
{
    private readonly DirectoryInfo _pluginDirectory;
    public bool ShowUnsupportedMods { get; set; } = false;
    private IModrinthApi _api;
    private string[] _pluginLoaders = new[] {"bukkit", "paper", "purpur", "spigot"};
    public Updater(string pluginDirectory)
    {
        this._pluginDirectory = new DirectoryInfo(pluginDirectory);

        _api = ModrinthApi.NewClient();
        
        if (_pluginDirectory.Exists == false)
        {
            throw new DirectoryNotFoundException($"Directory '{pluginDirectory}' does not exists.");
        }
    }

    private bool IsLatest(Version currentVersion, Version latestVersion)
    {
        return currentVersion.Id == latestVersion.Id && currentVersion.VersionNumber == latestVersion.VersionNumber;
    }

    private Version? FindLatestServerVersion(IEnumerable<Version> versionList)
    {
        var latestVersion = versionList.OrderByDescending(x => x.DatePublished)
            .FirstOrDefault(x => x.Loaders.Intersect(_pluginLoaders, StringComparer.InvariantCultureIgnoreCase).Any());

        return latestVersion;
    }

    public async Task<List<Plugin>> CheckUpdates()
    {
        if (_pluginDirectory is null)
        {
            throw new DirectoryNotFoundException();
        }

        var pluginFiles = _pluginDirectory.EnumerateFiles().Where(x => x.Extension == ".jar").ToList();
        
        var totalTicks = pluginFiles.Count;
        var options = new ProgressBarOptions
        {
            ProgressCharacter = '─',
            ProgressBarOnBottom = true,
            CollapseWhenFinished = true
        };

        using var pbar = new ProgressBar(totalTicks, "Update check started", options);
        
        var plugins = new List<Plugin>();
        using var cryptoService = SHA1.Create();
        foreach (var file in pluginFiles)
        {
            pbar.Tick($"Checking {file.Name} for updates");
            await using var stream = file.OpenRead();
            var hash = Convert.ToHexString(await cryptoService.ComputeHashAsync(stream));

            var plugin = new Plugin()
            {
                File = file,
                OnModrinth = false
            };
            try
            {
                var currentVersion = await _api.GetVersionByHashAsync(hash, HashAlgorithm.Sha1);

                var versionList = await _api.GetProjectVersionListAsync(currentVersion.ProjectId);

                var project = await _api.GetProjectAsync(currentVersion.ProjectId);

                var latestVersion = FindLatestServerVersion(versionList);

                if (latestVersion is null)
                {
                    //todo: Somehow write this error line
                    pbar.ObservedError = true;
                    pbar.WriteErrorLine($"Looks like {file.Name} is not a server plugin, please check the current version: {currentVersion.GetUrl(project)}");
                    continue;
                }

                if (IsLatest(currentVersion, latestVersion))
                {
                    plugin = new Plugin
                    {
                        File = file,
                        CurrentVersion = currentVersion,
                        LatestVersion = latestVersion,
                        Project = project,
                        UpToDate = true,
                        OnModrinth = true
                    };
                }
                else
                {
                    plugin = new Plugin
                    {
                        File = file,
                        CurrentVersion = currentVersion,
                        LatestVersion = latestVersion,
                        Project = project,
                        UpToDate = false,
                        OnModrinth = true
                    };
                }
            }
            // Not on Modrinth
            catch (ApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                plugin.OnModrinth = false;
            }
            catch (ApiException e)
            {
                Console.WriteLine($"Unknown error while checking update for file {file.Name}: {e.Message}");
            }
            
            plugins.Add(plugin);
        }

        pbar.Message = "Done";
        pbar.Dispose();
        Console.WriteLine();
        return plugins;
    }
    
    public static async Task CheckPluginUpdates(FileSystemInfo di, bool showAll)
    {
        var defaultColor = Console.ForegroundColor;

        var updater = new Updater(di.FullName);
    
        var plugins = await updater.CheckUpdates();
        var pluginsOnModrinth = plugins.OrderBy(x => x.OnModrinth).ThenBy(x => x.UpToDate).ToList();
    
        foreach (var plugin in pluginsOnModrinth)
        {
            if (plugin.OnModrinth == false)
            {
                if (showAll)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"X This version of plugin {plugin.File!.Name} is not on Modrinth");
                }
                continue;
            }

            if (plugin.UpToDate)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Plugin {plugin.File!.Name} is up to date");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(
                    $"Found new version for {plugin.File!.Name} => {plugin.LatestVersion!.VersionNumber} link: {plugin.LatestVersion.GetUrl(plugin.Project!)}");
            }
        }

        Console.ForegroundColor = defaultColor;
    }
    
    public static async Task UpdatePlugins(FileSystemInfo di)
    {
        Console.WriteLine("Checking plugins that need an update");
    
        var defaultColor = Console.ForegroundColor;
        var updater = new Updater(di.FullName);
        var plugins = await updater.CheckUpdates();

        DirectoryInfo oldPlugins = new DirectoryInfo("oldplugins");

        if (oldPlugins.Exists == false)
        {
            Console.WriteLine("Creating directory oldplugins");
            oldPlugins.Create();
        }

        using var client = new HttpClient();
        // Iterate not up-to-date plugins
        foreach (var plugin in plugins.Where(plugin => plugin.OnModrinth && !plugin.UpToDate))
        {
            if (plugin.LatestVersion!.Files.Length > 1)
            {
                // TODO: Better message
                Console.WriteLine($"I'm not sure which file to download, please download this plugin manually: {plugin.Project!.Title}");
                continue;
            }

            Console.WriteLine($"Downloading latest version of plugin {plugin.Project!.Title}");

            var file = plugin.LatestVersion.Files.First();

            Console.WriteLine($"Downloading plugin {plugin.Project!.Title}");
            var stream = await client.GetByteArrayAsync(file.Url);
        
            Console.WriteLine("Moving old version to oldplugins directory");
        
            //TODO: Check if file already exists
            File.Move(plugin.File!.FullName, Path.Combine(oldPlugins.FullName, plugin.File.Name));
            File.WriteAllBytes(Path.Combine(di.FullName, file.FileName), stream);

            Console.WriteLine($"Download of plugin {plugin.Project!.Title} completed");
            Console.WriteLine();
        }
    }
}