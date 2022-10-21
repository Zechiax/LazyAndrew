using System.CommandLine;
using LazyAndrew.Enums;
using Modrinth.RestClient.Extensions;

namespace LazyAndrew.Commands;

public class CheckCommand : Command
{
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly Option<DirectoryInfo> _directoryOption;
    private readonly Option<string> _versionOption;
    private readonly Option<bool> _showAllOption  =
        new(name: "--all", description: "If it should display mods that are not on Modrinth");
    public CheckCommand() : base("check", "Checks for updates for all plugins")
    {
        _showAllOption.AddAlias("-a");

        _directoryOption = Andrew.GetPluginDirectoryOption();
        _versionOption = Andrew.GetVersionOption();

        AddOption(_versionOption);
        AddOption(_showAllOption);
        AddOption(_directoryOption);
        
        this.SetHandler(async (directory, showAll, version) =>
        {
            await CheckPluginUpdates(directory, showAll, version);
        }, _directoryOption, _showAllOption, _versionOption);
    }
    
    private static async Task CheckPluginUpdates(FileSystemInfo di, bool showAll, string version)
    {
        var defaultColor = Console.ForegroundColor;

        var updater = new Updater(di.FullName, version);
    
        var statusList = await updater.CheckUpdates();
        var plugins = statusList.OrderByDescending(x => x.Status == CheckStatus.NotOnModrinth)
            .ThenBy(x => x.SuccessfulCheck).ThenBy(x => x.Status == CheckStatus.UpToDate).ToList();
    
        foreach (var status in plugins)
        {
            var plugin = status.Payload!;
            if (status.Status == CheckStatus.NotOnModrinth)
            {
                if (showAll)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"X This version of plugin {status.Name} is not on Modrinth");
                }
                continue;
            }

            if (status.Status == CheckStatus.ClientOnly)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"The plugin {plugin.File.Name} is on Modrinth, but I couldn't find any latest server version, please, check manually for the latest version: {plugin.Project?.Url}");
                continue;
            }

            if (status.Status == CheckStatus.UpToDate)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Plugin {plugin.File!.Name} is up to date");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(
                    $"Found new version for {plugin.File.Name} => {plugin.LatestVersion!.VersionNumber} ({plugin.LatestVersion!}) link: {plugin.LatestVersion.GetUrl(plugin.Project!)}");
            }
        }

        Console.ForegroundColor = defaultColor;
    }
}