// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Cli;

/// <summary>
/// Checks NuGet for the latest available version of a package and caches the result.
/// The check runs at most once per configured interval to avoid slowing down every command.
/// </summary>
public static class UpdateChecker
{
    /// <summary>
    /// The NuGet package ID for the CLI tool.
    /// </summary>
    public const string CliPackageId = "Cratis.Chronicle.Cli";

    /// <summary>
    /// The NuGet package ID used as a proxy for the Chronicle server version.
    /// The server ships as a Docker image but shares the same release version as this client library.
    /// </summary>
    public const string ServerPackageId = "Cratis.Chronicle";

    static readonly TimeSpan _checkInterval = TimeSpan.FromHours(24);
    static readonly JsonSerializerOptions _cacheJsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// Gets the path to the cached version check file.
    /// </summary>
    /// <returns>The absolute file path.</returns>
    public static string GetCachePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cratis", "version-check.json");

    /// <summary>
    /// Checks whether a newer version of the CLI is available.
    /// </summary>
    /// <param name="currentVersion">The current CLI version.</param>
    /// <param name="cancellationToken">A cancellation token for timeout control.</param>
    /// <returns>The latest version string if newer, otherwise null.</returns>
    public static Task<string?> CheckForUpdate(string currentVersion, CancellationToken cancellationToken = default)
        => CheckForUpdate(CliPackageId, currentVersion, cancellationToken);

    /// <summary>
    /// Checks whether a newer version of the specified NuGet package is available.
    /// Returns the latest version string if an update is available, or null if the
    /// package is up to date or the check fails. Designed to be called with a short
    /// timeout so it never blocks the user.
    /// </summary>
    /// <param name="packageId">The NuGet package ID to check.</param>
    /// <param name="currentVersion">The current version.</param>
    /// <param name="cancellationToken">A cancellation token for timeout control.</param>
    /// <returns>The latest version string if newer, otherwise null.</returns>
    public static async Task<string?> CheckForUpdate(string packageId, string currentVersion, CancellationToken cancellationToken = default)
    {
        var cache = ReadCache();
        if (cache?.Packages.TryGetValue(packageId, out var entry) == true &&
            DateTime.UtcNow - entry.CheckedAt < _checkInterval)
        {
            return IsNewer(entry.LatestVersion, currentVersion) ? entry.LatestVersion : null;
        }

        try
        {
            var latestVersion = await FetchLatestVersion(packageId, cancellationToken);
            if (latestVersion is null)
            {
                return null;
            }

            cache ??= new VersionCache();
            cache.Packages[packageId] = new PackageVersionEntry
            {
                LatestVersion = latestVersion,
                CheckedAt = DateTime.UtcNow
            };
            WriteCache(cache);

            return IsNewer(latestVersion, currentVersion) ? latestVersion : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Determines whether the latest version is newer than the current version.
    /// </summary>
    /// <param name="latest">The latest available version.</param>
    /// <param name="current">The current version (may include prerelease suffix).</param>
    /// <returns>True if the latest version is strictly greater than the current version.</returns>
    internal static bool IsNewer(string latest, string current)
    {
        var dashIndex = current.IndexOf('-');
        var currentNumeric = dashIndex > 0 ? current[..dashIndex] : current;

        return Version.TryParse(latest, out var latestVer) &&
               Version.TryParse(currentNumeric, out var currentVer) &&
               latestVer > currentVer;
    }

    static async Task<string?> FetchLatestVersion(string packageId, CancellationToken cancellationToken)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var url = $"https://api.nuget.org/v3-flatcontainer/{packageId.ToLowerInvariant()}/index.json";
        var response = await http.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty("versions", out var versions) ||
            versions.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        // NuGet returns versions in ascending order; the last stable version is what we want.
        string? latest = null;

        foreach (var v in versions.EnumerateArray())
        {
            var versionString = v.GetString();
            if (versionString?.Contains('-') == false)
            {
                latest = versionString;
            }
        }

        return latest;
    }

    static VersionCache? ReadCache()
    {
        var path = GetCachePath();
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<VersionCache>(json);
        }
        catch
        {
            return null;
        }
    }

    static void WriteCache(VersionCache cache)
    {
        try
        {
            var path = GetCachePath();
            var directory = Path.GetDirectoryName(path)!;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(cache, _cacheJsonOptions);
            File.WriteAllText(path, json);
        }
        catch
        {
            // Cache write failure is non-critical.
        }
    }

    sealed class VersionCache
    {
        public Dictionary<string, PackageVersionEntry> Packages { get; set; } = new();
    }

    sealed class PackageVersionEntry
    {
        public string LatestVersion { get; set; } = string.Empty;
        public DateTime CheckedAt { get; set; }
    }
}
