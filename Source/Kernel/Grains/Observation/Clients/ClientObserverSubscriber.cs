// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Grains.Observation.Placement;
using Cratis.Chronicle.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation.Clients;

/// <summary>
/// Represents an implementation of <see cref="IClientObserverSubscriber"/>.
/// </summary>
/// <remarks>
/// We want this grain to be local to its activation. When a client connects, the service instance that
/// receives the connection will activate this grain and we then want it to be local to that service instance
/// and not perform a network hop. The <see cref="ObserverKey"/> contains a <see cref="ConnectionId"/> which
/// will make the observer unique per connection, helping us to achieve this.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="ClientObserverSubscriber"/> class.
/// </remarks>
/// <param name="observerMediator"><see cref="IObserverMediator"/> for notifying actual clients.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
[ConnectedObserverPlacement]
public class ClientObserverSubscriber(
    IObserverMediator observerMediator,
    ILogger<ClientObserverSubscriber> logger) : Grain, IClientObserverSubscriber
{
    EventStoreName _eventStore = EventStoreName.NotSet;
    ObserverId _observerId = ObserverId.Unspecified;
    EventStoreNamespaceName _namespace = EventStoreNamespaceName.NotSet;
    EventSequenceId _eventSequenceId = EventSequenceId.Unspecified;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var key = ObserverSubscriberKey.Parse(this.GetPrimaryKeyString());
        _observerId = key.ObserverId;
        _eventStore = key.EventStore;
        _namespace = key.Namespace;
        _eventSequenceId = key.EventSequenceId;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<ObserverSubscriberResult> OnNext(IEnumerable<AppendedEvent> events, ObserverSubscriberContext context)
    {
        foreach (var @event in events)
        {
            logger.EventReceived(
                _observerId,
                _eventStore,
                _namespace,
                @event.Metadata.Type.Id,
                _eventSequenceId,
                @event.Context.SequenceNumber);
        }

        if (context.Metadata is not ConnectedClient connectedClient)
        {
            throw new MissingStateForObserverSubscriber(_observerId);
        }
        var tcs = new TaskCompletionSource<ObserverSubscriberResult>();
        try
        {
            observerMediator.OnNext(
                _observerId,
                connectedClient.ConnectionId,
                events,
                tcs);
            return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (TaskCanceledException)
        {
            return ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "Task was cancelled");
        }
        catch (TimeoutException)
        {
            return ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "Timeout");
        }
    }
}
