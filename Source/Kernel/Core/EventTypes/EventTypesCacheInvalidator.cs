// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Orleans.BroadcastChannel;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Represents an implementation of <see cref="IEventTypesCacheInvalidator"/> that evicts the local event
/// type storage cache in response to <see cref="EventTypesChanged"/> broadcasts.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for reaching the event store's event type storage.</param>
[ImplicitChannelSubscription]
public class EventTypesCacheInvalidator(IStorage storage) : Grain, IEventTypesCacheInvalidator, IOnBroadcastChannelSubscribed
{
    EventStoreName _eventStoreName = EventStoreName.NotSet;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _eventStoreName = this.GetPrimaryKeyString();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Ensure() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task OnSubscribed(IBroadcastChannelSubscription streamSubscription)
    {
        var eventStore = streamSubscription.ChannelId.GetKeyAsString();
        if (_eventStoreName != eventStore) return Task.CompletedTask;

        streamSubscription.Attach<EventTypesChanged>(OnEventTypesChanged, OnError);
        return Task.CompletedTask;
    }

    Task OnEventTypesChanged(EventTypesChanged changed)
    {
        storage.GetEventStore(_eventStoreName).EventTypes.Invalidate(changed.EventTypeId);
        return Task.CompletedTask;
    }

    Task OnError(Exception exception) => Task.CompletedTask;
}
