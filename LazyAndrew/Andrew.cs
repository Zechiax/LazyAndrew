﻿using System.CommandLine;
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

    private readonly RootCommand _rootCommand;

    public Andrew()
    {
        _rootCommand = new RootCommand("Lazy Andrew");

        _rootCommand.AddCommand(new CheckCommand());
        _rootCommand.AddCommand(new UpdateCommand());
    }

    public async Task<int> StartAsync()
    {
        return await _rootCommand.InvokeAsync(Environment.GetCommandLineArgs().Skip(1).ToArray());
    }
}