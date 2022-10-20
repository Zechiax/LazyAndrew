using System.CommandLine;

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
            await Updater.CheckPluginUpdates(directory, showAll);
        }, _directoryOption, _showAllOption);
    }
}