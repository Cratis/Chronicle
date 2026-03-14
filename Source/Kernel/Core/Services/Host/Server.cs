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
        var serverAssembly = typeof(Server).Assembly;
        var contractsAssembly = typeof(IServer).Assembly;

        return Task.FromResult(new ServerVersionInfo
        {
            Version = GetVersionFromAssembly(serverAssembly),
            ContractsVersion = GetVersionFromAssembly(contractsAssembly),
            CommitSha = GetCommitShaFromAssembly(serverAssembly)
        });
    }

    static string GetVersionFromAssembly(Assembly assembly)
    {
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (informational is not null)
        {
            var version = informational.InformationalVersion;
            var plusIndex = version.IndexOf('+');

            return plusIndex > 0 ? version[..plusIndex] : version;
        }

        return assembly.GetName().Version?.ToString() ?? "0.0.0";
    }

    static string GetCommitShaFromAssembly(Assembly assembly)
    {
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (informational is not null)
        {
            var version = informational.InformationalVersion;
            var plusIndex = version.IndexOf('+');

            if (plusIndex >= 0 && plusIndex < version.Length - 1)
            {
                return version[(plusIndex + 1)..];
            }
        }

        return string.Empty;
    }
}
