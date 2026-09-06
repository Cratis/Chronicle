// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.IO.Compression;
using System.Net.Http.Json;
using System.Text.Json;

namespace Cratis.Chronicle.Tools.WireCompatibility;

/// <summary>
/// Fetches a released Chronicle contracts assembly from NuGet.
/// </summary>
/// <param name="client">The <see cref="HttpClient"/> to fetch with.</param>
/// <remarks>
/// The baseline for the release gate is a package that is already published and can never change, which is what
/// makes it a usable fixed point: whatever is on the shelf is what someone out there is running.
/// </remarks>
public sealed class NuGetContractsPackage(HttpClient client) : IDisposable
{
    const string PackageId = "cratis.chronicle.contracts";
    const string FlatContainer = "https://api.nuget.org/v3-flatcontainer";

    static readonly string[] _preferredTargetFrameworks = ["net10.0", "net9.0", "net8.0"];

    readonly List<string> _temporaryDirectories = [];

    /// <summary>
    /// Resolves the earliest stable release of every minor within a major, oldest first.
    /// </summary>
    /// <param name="major">The major version.</param>
    /// <returns>One baseline version per released minor.</returns>
    /// <exception cref="NoReleaseForMajor">Thrown when nothing stable was ever published for that major.</exception>
    /// <remarks>
    /// The first release of the major is the strictest baseline for anything that existed from the start, but it is
    /// not the only one that matters: something added in 16.20 and removed since breaks every client on 16.20 or
    /// later while 16.0.0 never notices. One baseline per minor covers that, and reporting each separately says
    /// <em>when</em> a contract broke rather than only that it did.
    /// <para>
    /// The earliest release of each minor is the one taken, because it is the one every later patch had to keep
    /// serving. Not every minor starts at <c>x.y.0</c> - 11 starts at 11.0.1, 12 at 12.0.2, 15 at 15.0.3 - so this
    /// reads what was actually published rather than assuming a version number exists.
    /// </para>
    /// </remarks>
    public async Task<IReadOnlyList<string>> StableReleasePerMinor(int major)
    {
        var index = await client.GetFromJsonAsync<JsonElement>($"{FlatContainer}/{PackageId}/index.json");
        var prefix = $"{major.ToString(CultureInfo.InvariantCulture)}.";

        var perMinor = index.GetProperty("versions")
            .EnumerateArray()
            .Select(_ => _.GetString() ?? string.Empty)
            .Where(_ => _.StartsWith(prefix, StringComparison.Ordinal) && !_.Contains('-', StringComparison.Ordinal))

            // The flat container lists versions in ascending order, so the first entry seen for a minor is its
            // earliest release.
            .GroupBy(MinorOf)
            .Select(_ => _.First())
            .ToArray();

        return perMinor.Length > 0 ? perMinor : throw new NoReleaseForMajor(major);
    }

    /// <summary>
    /// Downloads a released contracts package and extracts the assembly from it.
    /// </summary>
    /// <param name="version">The version to download.</param>
    /// <returns>The path to the extracted contracts assembly.</returns>
    /// <exception cref="ContractsAssemblyNotInPackage">Thrown when the package holds no usable contracts assembly.</exception>
    public async Task<string> DownloadAssembly(string version)
    {
        var directory = Directory.CreateTempSubdirectory("chronicle-wire-baseline-").FullName;
        _temporaryDirectories.Add(directory);

        var lowered = version.ToLowerInvariant();
        await using var package = await client.GetStreamAsync($"{FlatContainer}/{PackageId}/{lowered}/{PackageId}.{lowered}.nupkg");
        await using var archive = new ZipArchive(package, ZipArchiveMode.Read);

        var entry = _preferredTargetFrameworks
            .Select(tfm => archive.GetEntry($"lib/{tfm}/Cratis.Chronicle.Contracts.dll"))
            .FirstOrDefault(_ => _ is not null)
            ?? archive.Entries.FirstOrDefault(_ => _.FullName.EndsWith("/Cratis.Chronicle.Contracts.dll", StringComparison.OrdinalIgnoreCase))
            ?? throw new ContractsAssemblyNotInPackage(version);

        var path = Path.Combine(directory, "Cratis.Chronicle.Contracts.dll");
        await entry.ExtractToFileAsync(path);
        return path;
    }

    /// <summary>
    /// Removes everything that was downloaded.
    /// </summary>
    public void Dispose()
    {
        foreach (var directory in _temporaryDirectories.Where(Directory.Exists))
        {
            Directory.Delete(directory, recursive: true);
        }

        _temporaryDirectories.Clear();
        client.Dispose();
    }

    static string MinorOf(string version)
    {
        var parts = version.Split('.');
        return parts.Length >= 2 ? $"{parts[0]}.{parts[1]}" : version;
    }
}
