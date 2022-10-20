using System.CommandLine;

namespace LazyAndrew.Commands;

public class UpdateCommand : Command
{
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly Option<DirectoryInfo> _directoryOption;
    public UpdateCommand() : base("update", "update all plugins that have updates")
    {
        _directoryOption = Andrew.GetPluginDirectoryOption();
        
        AddOption(_directoryOption);
        
        this.SetHandler(async directory =>
        {
            await UpdatePlugins(directory);
        }, _directoryOption);
    }

    private static async Task UpdatePlugins(FileSystemInfo di)
    {
        Console.WriteLine("Checking plugins that need an update");

        var defaultColor = Console.ForegroundColor;
        var updater = new Updater(di.FullName);
        var plugins = await updater.CheckUpdates();

        var oldPlugins = new DirectoryInfo("oldplugins");

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
                Console.WriteLine(
                    $"I'm not sure which file to download, please download this plugin manually: {plugin.Project!.Title}");
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