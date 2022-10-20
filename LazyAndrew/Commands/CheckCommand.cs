using System.CommandLine;
using Modrinth.RestClient.Extensions;

namespace LazyAndrew.Commands;

public class CheckCommand : Command
{
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly Option<DirectoryInfo> _directoryOption;
    private readonly Option<bool> _showAllOption  =
        new(name: "--all", description: "If it should display mods that are not on Modrinth");
    public CheckCommand() : base("check", "Checks for updates for all plugins")
    {
        _showAllOption.AddAlias("-a");

        _directoryOption = Andrew.GetPluginDirectoryOption();

        AddOption(_showAllOption);
        AddOption(_directoryOption);
        
        this.SetHandler(async (directory, showAll) =>
        {
            await CheckPluginUpdates(directory, showAll);
        }, _directoryOption, _showAllOption);
    }
    
    private static async Task CheckPluginUpdates(FileSystemInfo di, bool showAll)
    {
        var defaultColor = Console.ForegroundColor;

        var updater = new Updater(di.FullName);
    
        var statusList = await updater.CheckUpdates();
        var pluginsOnModrinth = statusList.OrderBy(x => x.SuccessfulCheck && x.Payload.OnModrinth).ThenBy(x => x.Payload.UpToDate).ToList();
    
        foreach (var plugin in pluginsOnModrinth.Select(status => status.Payload))
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
}