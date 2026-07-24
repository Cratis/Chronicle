// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Orleans.BroadcastChannel;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Represents an implementation of <see cref="IEventTypesChangedNotifier"/> that publishes on the
/// <see cref="WellKnownBroadcastChannelNames.EventTypesChanged"/> broadcast channel.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventTypesChangedNotifier"/> class.
/// </remarks>
/// <param name="clusterClient"><see cref="IClusterClient"/> for getting the broadcast channel provider.</param>
public class EventTypesChangedNotifier(IClusterClient clusterClient) : IEventTypesChangedNotifier
{
    readonly IBroadcastChannelProvider _channel = clusterClient.GetBroadcastChannelProvider(WellKnownBroadcastChannelNames.EventTypesChanged);

    /// <inheritdoc/>
    public async Task Notify(EventStoreName eventStore, EventTypeId eventTypeId)
    {
        var channelId = ChannelId.Create(WellKnownBroadcastChannelNames.EventTypesChanged, eventStore);
        var channelWriter = _channel.GetChannelWriter<EventTypesChanged>(channelId);
        await channelWriter.Publish(new EventTypesChanged(eventStore, eventTypeId));
    }
}
