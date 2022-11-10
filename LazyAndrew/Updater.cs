using System.Net;
using System.Security.Cryptography;
using LazyAndrew.Enums;
using LazyAndrew.Exceptions;
using LazyAndrew.Interfaces;
using LazyAndrew.Models;
using Modrinth.RestClient;
using Modrinth.RestClient.Extensions;
using Modrinth.RestClient.Models.Enums;
using RestEase;
using Serilog;
using Serilog.Core;
using ShellProgressBar;
using HashAlgorithm = Modrinth.RestClient.Models.Enums.HashAlgorithm;
using Version = Modrinth.RestClient.Models.Version;

namespace LazyAndrew;

public class Updater
{
    private readonly DirectoryInfo _pluginDirectory;
    private IModrinthApi _api;
    private readonly string[] _pluginLoaders = {"bukkit", "spigot", "paper", "purpur"};
    private readonly string[] _serverModLoaders = {"fabric", "quilt", "forge"};

    private readonly CryptoService _cryptoService;
    private readonly string _targetGameVersion;
    public Updater(string pluginDirectory, string version = "latest")
    {
        _pluginDirectory = new DirectoryInfo(pluginDirectory);

        _cryptoService = new CryptoService();
        _api = ModrinthApi.NewClient();

        if (version == "latest")
        {
            _targetGameVersion = GetLatestGameVersion().GetAwaiter().GetResult();
            Log.Debug("Set the target version to latest version: {Latest}", _targetGameVersion);
        }
        else
        {
            if (CheckMcVersion(version).GetAwaiter().GetResult() == false)
            {
                throw new UnsupportedVersionException($"Minecraft version '{version}' is not supported");
            }

            _targetGameVersion = version;
            Log.Debug("Set the target version to custom target version: {Latest}", _targetGameVersion);
        }

        if (_pluginDirectory.Exists == false)
        {
            throw new DirectoryNotFoundException($"Directory '{pluginDirectory}' does not exists.");
        }
    }

    private async Task<string> GetLatestGameVersion()
    {
        var gameVersions = await _api.GetGameVersionsAsync();
        return gameVersions.OrderByDescending(x => x.Date).First(x => x.VersionType == GameVersionType.Release).Version;
    }

    private async Task<bool> CheckMcVersion(string version)
    {
        var gameVersions = await _api.GetGameVersionsAsync();

        var foundVersion = gameVersions.FirstOrDefault(x => x.Version == version);

        return foundVersion is not null;
    }

    private bool IsLatest(Version currentVersion, Version latestVersion)
    {
        return currentVersion.Id == latestVersion.Id && currentVersion.VersionNumber == latestVersion.VersionNumber;
    }

    private Version? FindLatestServerVersion(IEnumerable<Version> versionList)
    {
        var latestVersion = versionList.OrderByDescending(x => x.DatePublished)
            .Where(x => x.GameVersions.Contains(_targetGameVersion))
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

    private async Task GetUpdateInformation(IReadOnlyCollection<IUpdateStatus<PluginDto>> plugins)
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
            
            if (project.ServerSide == Side.Unsupported || latestVersion is null)
            {
                updateStatus.Status = CheckStatus.ClientOnly;
                payload.Project = project;
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

    private async Task CheckPlugins(ICollection<IUpdateStatus<PluginDto>> plugins)
    {
        var pluginFiles = GetJarFilesInPluginDirectory();

        foreach (var file in pluginFiles)
        {
            var status = new UpdateStatus();
            await using var stream = file.OpenRead();

            Log.Debug("Checking file {FileName}", file.Name);
            var hash = _cryptoService.ComputeHashAsync(stream, Enums.HashAlgorithm.sha1);;

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
                Log.Debug("File {FileName} is not on Modrinth", file.Name);
                status.Name = file.Name;
                status.SuccessfulCheck = true;
                status.Status = CheckStatus.NotOnModrinth;
            }
            catch (Exception e)
            {
                Log.Warning("Update check failed for file {FileName}", file.Name);
                status.Name = file.Name;
                status.ErrorMessage = e.Message;
                status.SuccessfulCheck = false;
            }

            await stream.DisposeAsync();
            
            plugins.Add(status);
        }
    }
}