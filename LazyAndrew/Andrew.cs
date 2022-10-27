using System.CommandLine;
using LazyAndrew.Commands;

namespace LazyAndrew;

public class Andrew
{
    /// <summary>
    /// Creates and returns new option for setting the plugin directory
    /// </summary>
    /// <returns></returns>
    public static Option<DirectoryInfo> GetPluginDirectoryOption()
    {
        var option = new Option<DirectoryInfo>(
            name: "--directory",
            description: "The plugins directory")
        {
            IsRequired = true
        };

        option.AddAlias("-d");
        
        return option;
    }
    
    /// <summary>
    /// Creates and returns new option for setting the plugin directory
    /// </summary>
    /// <returns></returns>
    public static Option<string> GetVersionOption()
    {
        var option = new Option<string>(
            name: "--version",
            description: "Target minecraft version (default is the latest)")
        {
            IsRequired = true
        };

        option.SetDefaultValue("latest");
        option.AddAlias("-v");
        
        return option;
    }

    private readonly RootCommand _rootCommand;

    public Andrew()
    {
        _rootCommand = new RootCommand("Lazy Andrew");

        _rootCommand.AddCommand(new CheckCommand());
        _rootCommand.AddCommand(new UpdateCommand());
        _rootCommand.AddCommand(new InitializeCommand());
    }

    public async Task<int> StartAsync()
    {
        // Skip 1 as the first argument is the program's directory
        return await _rootCommand.InvokeAsync(Environment.GetCommandLineArgs().Skip(1).ToArray());
    }
}