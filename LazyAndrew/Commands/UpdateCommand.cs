using System.CommandLine;
using LazyAndrew.Enums;
using LazyAndrew.Exceptions;

namespace LazyAndrew.Commands;

public class UpdateCommand : Command
{
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly Option<DirectoryInfo> _directoryOption;
    private readonly FileDownloader _downloader;
    public UpdateCommand() : base("update", "update all plugins that have updates")
    {
        _downloader = new FileDownloader();
        _directoryOption = Andrew.GetPluginDirectoryOption();
        
        AddOption(_directoryOption);
        
        this.SetHandler(async directory =>
        {
            await UpdatePlugins(directory);
        }, _directoryOption);
    }

    private async Task UpdatePlugins(FileSystemInfo di)
    {
        Console.WriteLine("Checking plugins that need an update");

        var defaultColor = Console.ForegroundColor;
        var updater = new Updater(di.FullName);
        var statusList = await updater.CheckUpdates();

        var oldPlugins = new DirectoryInfo("oldplugins");

        if (oldPlugins.Exists == false)
        {
            Console.WriteLine("Creating directory oldplugins");
            oldPlugins.Create();
        }
        // Iterate not up-to-date plugins
        foreach (var plugin in statusList.Where(x => x.SuccessfulCheck && x.Status == CheckStatus.NewerVersionFound).Select(status => status.Payload))
        {
            if (plugin!.LatestVersion!.Files.Length > 1)
            {
                // TODO: Better message
                Console.WriteLine(
                    $"I'm not sure which file to download, please download this plugin manually: {plugin.Project!.Title}");
                continue;
            }

            Console.WriteLine($"Downloading latest version of plugin {plugin.Project!.Title}");

            var file = plugin.LatestVersion.Files.First();

            Console.WriteLine($"Downloading plugin {plugin.Project!.Title}");

            try
            {
                var downloadedFile = await _downloader.DownloadFile(file.Url, file.Hashes.Sha512, HashAlgorithm.sha512);
                Console.WriteLine("File Hash verified");
                Console.WriteLine($"Download of plugin {plugin.Project!.Title} completed");
            
                Console.WriteLine("Moving old version to oldplugins directory");

                //TODO: Check if file already exists
                File.Move(plugin.File.FullName, Path.Combine(oldPlugins.FullName, plugin.File.Name));
                
                File.Move(downloadedFile.FullName, Path.Combine(di.FullName, file.FileName));

                Console.WriteLine("Plugin updated");
                Console.WriteLine();
            }
            catch (HashNotVerifiedException)
            {
                Console.WriteLine("Could not verify file hash, aborting download");
            }
        }
    }
}