namespace LazyAndrew.Enums;

public enum CheckStatus
{
    /// <summary>
    /// Newer version of the plugin was found
    /// </summary>
    NewerVersionFound,
    /// <summary>
    /// The plugin is up-to-date
    /// </summary>
    UpToDate,
    /// <summary>
    /// The version of this plugin is not on Modrinth
    /// </summary>
    NotOnModrinth,
    PendingCheck,
    /// <summary>
    /// The updater couldn't find the latest server version for this project
    /// </summary>
    ClientOnly
}