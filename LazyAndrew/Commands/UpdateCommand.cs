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
            await Updater.UpdatePlugins(directory);
        }, _directoryOption);
    }
}