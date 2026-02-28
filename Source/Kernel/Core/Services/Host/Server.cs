// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
}
