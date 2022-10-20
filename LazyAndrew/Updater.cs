﻿using System.Net;
using System.Security.Cryptography;
using LazyAndrew.Enums;
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
    private IModrinthApi _api;
    private string[] _pluginLoaders = new[] {"bukkit", "paper", "purpur", "spigot"};
    private SHA1 _cryptoService;
    public Updater(string pluginDirectory)
    {
        _cryptoService = SHA1.Create();
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

    private List<FileInfo> GetJarFilesInPluginDirectory()
    {
        return _pluginDirectory.EnumerateFiles().Where(x => x.Extension == ".jar").ToList();
    }
    
    public async Task<List<IUpdateStatus<PluginDto>>> CheckUpdates()
    {
        if (_pluginDirectory is null)
        {
            throw new DirectoryNotFoundException();
        }

        var plugins = new List<IUpdateStatus<PluginDto>>();
        
        await CheckPlugins(plugins);
        await GetUpdateInformation(plugins);

        return plugins;
    }

    private async Task GetUpdateInformation(List<IUpdateStatus<PluginDto>> plugins)
    {
        // Select project ids
        var projectIds = plugins.Where(x => x.SuccessfulCheck && x.Status == CheckStatus.PendingCheck)
            .Select(x => x.Payload!.CurrentVersion.ProjectId);

        var projects = await _api.GetMultipleProjectsAsync(projectIds);
        
        foreach (var updateStatus in plugins.Where(x => x.SuccessfulCheck && x.Status == CheckStatus.PendingCheck))
        {
            var payload = updateStatus.Payload!;
            
            var project = projects.First(x => x.Id == payload.CurrentVersion.ProjectId);
            payload.Project = project;
            
            var versionList = await _api.GetProjectVersionListAsync(project.Id);
            var latestVersion = FindLatestServerVersion(versionList);
            
            if (latestVersion is null)
            {
                updateStatus.Status = CheckStatus.NoLatest;
                continue;
            }

            payload.LatestVersion = latestVersion;

            if (IsLatest(updateStatus.Payload!.CurrentVersion, latestVersion))
            {
                updateStatus.Status = CheckStatus.UpToDate;
            }
            else
            {
                updateStatus.Status = CheckStatus.NewerVersionFound;
            }
        }
    }

    private async Task CheckPlugins(List<IUpdateStatus<PluginDto>> plugins)
    {
        var pluginFiles = GetJarFilesInPluginDirectory();
        
        foreach (var file in pluginFiles)
        {
            var status = new UpdateStatus();
            await using var stream = file.OpenRead();

            var hash = await GetStringHash(stream);

            try
            {
                var currentVersion = await _api.GetVersionByHashAsync(hash, HashAlgorithm.Sha1);
                status.Payload = new PluginDto(currentVersion, file);
                status.SuccessfulCheck = true;
                status.Status = CheckStatus.PendingCheck;
            }
            // This file is not on Modrinth
            catch (ApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                status.Name = file.Name;
                status.SuccessfulCheck = true;
                status.Status = CheckStatus.NotOnModrinth;
            }
            catch (Exception e)
            {
                status.Name = file.Name;
                status.ErrorMessage = e.Message;
                status.SuccessfulCheck = false;
            }

            await stream.DisposeAsync();
            
            plugins.Add(status);
        }
    }

    private async Task<string> GetStringHash(FileStream stream)
    {
        return Convert.ToHexString(await _cryptoService.ComputeHashAsync(stream));
    }
}