﻿using System.Net;
using System.Security.Cryptography;
using LazyAndrew.Interfaces;
using LazyAndrew.Models;
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

    public async Task<List<IUpdateStatus<PluginDto>>> CheckUpdates()
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
        
        var plugins = new List<IUpdateStatus<PluginDto>>();
        using var cryptoService = SHA1.Create();
        foreach (var file in pluginFiles)
        {
            pbar.Tick($"Checking {file.Name} for updates");
            await using var stream = file.OpenRead();
            var hash = Convert.ToHexString(await cryptoService.ComputeHashAsync(stream));

            var plugin = new PluginDto()
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
                    pbar.ObservedError = true;
                    pbar.WriteErrorLine($"Looks like {file.Name} is not a server plugin, please check the current version: {currentVersion.GetUrl(project)}");
                    continue;
                }

                if (IsLatest(currentVersion, latestVersion))
                {
                    plugin = new PluginDto
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
                    plugin = new PluginDto
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
            
            plugins.Add(new UpdateStatus()
            {
                
            });
        }

        pbar.Message = "Done";
        pbar.Dispose();
        Console.WriteLine();
        return plugins;
    }
}