// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Threading.Channels;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_EventStoreSubscriptions.when_generation_is_pinned;

public class SubscriptionDeliveryTracker
{
    readonly ConcurrentDictionary<string, Channel<(string Name, EventContext Context)>> _deliveries = new();
    public Task Delivered(string name, EventContext context) => ChannelFor(context.EventStore.Value).Writer.WriteAsync((name, context)).AsTask();
    public async Task<(string Name, EventContext Context)> Next(string eventStore)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        return await ChannelFor(eventStore).Reader.ReadAsync(deadline.Token);
    }
    Channel<(string Name, EventContext Context)> ChannelFor(string eventStore) => _deliveries.GetOrAdd(eventStore, _ => Channel.CreateUnbounded<(string Name, EventContext Context)>());
}
