// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Contracts.Host;
using Cratis.Chronicle.Host;
using Orleans.BroadcastChannel;

namespace Cratis.Chronicle.Services.Host;

/// <summary>
/// Represents an implementation of <see cref="IServer"/>.
/// </summary>
/// <param name="clusterClient"><see cref="IClusterClient"/> instance.</param>
internal sealed class Server(IClusterClient clusterClient) : IServer
{
    readonly IBroadcastChannelProvider _reloadStateChannel = clusterClient.GetBroadcastChannelProvider(WellKnownBroadcastChannelNames.ReloadState);

    /// <inheritdoc/>
    public async Task ReloadState()
    {
        var channelId = ChannelId.Create(WellKnownBroadcastChannelNames.ReloadState, "Server");
        var channelWriter = _reloadStateChannel.GetChannelWriter<ReloadState>(channelId);
        await channelWriter.Publish(new ReloadState());
    }

    /// <inheritdoc/>
    public Task<ServerVersionInfo> GetVersionInfo()
    {
        // The entry assembly is the Chronicle server executable, which is versioned with the
        // actual release version by the publish pipeline (-p:Version=). The Core assembly
        // (typeof(Server).Assembly) is a library dependency whose version defaults to 1.0.0.
        var serverAssembly = Assembly.GetEntryAssembly() ?? typeof(Server).Assembly;
        var informational = serverAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        var informationalVersion = informational?.InformationalVersion
            ?? serverAssembly.GetName().Version?.ToString()
            ?? "0.0.0";

        return Task.FromResult(new ServerVersionInfo
        {
            Version = ParseVersionFromInformationalVersion(informationalVersion),
            CommitSha = ParseCommitShaFromInformationalVersion(informationalVersion)
        });
    }

    /// <summary>
    /// Parses the version portion from an assembly informational version string.
    /// Strips any build metadata or commit SHA (everything after the '+' separator).
    /// </summary>
    /// <param name="informationalVersion">The informational version string (e.g. "15.9.0+abc123").</param>
    /// <returns>The version portion before any '+' separator, or the full string if none is present.</returns>
    internal static string ParseVersionFromInformationalVersion(string informationalVersion)
    {
        var plusIndex = informationalVersion.IndexOf('+');
        return plusIndex > 0 ? informationalVersion[..plusIndex] : informationalVersion;
    }

    /// <summary>
    /// Parses the commit SHA from an assembly informational version string.
    /// Extracts the build metadata portion after the '+' separator.
    /// </summary>
    /// <param name="informationalVersion">The informational version string (e.g. "15.9.0+abc123").</param>
    /// <returns>The commit SHA after the '+' separator, or an empty string if none is present.</returns>
    internal static string ParseCommitShaFromInformationalVersion(string informationalVersion)
    {
        var plusIndex = informationalVersion.IndexOf('+');
        if (plusIndex >= 0 && plusIndex < informationalVersion.Length - 1)
        {
            return informationalVersion[(plusIndex + 1)..];
        }

        return string.Empty;
    }
}
