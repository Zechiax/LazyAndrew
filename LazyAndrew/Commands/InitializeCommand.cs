using System.CommandLine;
using Serilog;

namespace LazyAndrew.Commands;

public class InitializeCommand : Command
{
    private static Option<bool> GetForceOption()
    {
        var option = new Option<bool>(
            name: "--force",
            description: "Recreate the config, even if it already exists")
        {
            IsRequired = false
        };

        option.AddAlias("-f");
        
        return option;
    }
    public InitializeCommand(): base("initialize", "Interactive creation of configuration")
    {
        AddAlias("init");

        var forceOption = GetForceOption();
        AddOption(forceOption);
        
        this.SetHandler(async forceCreate =>
        {
            await CreateConfig(forceCreate);
        }, forceOption);
    }

    private async Task CreateConfig(bool forceCreate)
    {
        Log.Debug("Interactive config creation, forceCreate: {Force}", forceCreate);
        
    }
}