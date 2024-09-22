// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grains.Observation.Placement;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation.Reactors.Clients;

/// <summary>
/// Represents an implementation of <see cref="IReactorObserverSubscriber"/>.
/// </summary>
/// <remarks>
/// We want this grain to be local to its activation. When a client connects, the service instance that
/// receives the connection will activate this grain and we then want it to be local to that service instance
/// and not perform a network hop. The <see cref="ObserverKey"/> contains a <see cref="ConnectionId"/> which
/// will make the observer unique per connection, helping us to achieve this.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="ReactorObserverSubscriber"/> class.
/// </remarks>
/// <param name="reactorMediator"><see cref="IReactorMediator"/> for notifying actual clients.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
[ConnectedObserverPlacement]
public class ReactorObserverSubscriber(
    IReactorMediator reactorMediator,
    ILogger<ReactorObserverSubscriber> logger) : Grain, IReactorObserverSubscriber
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
            throw new MissingStateForReactorSubscriber(_observerId);
        }
        var tcs = new TaskCompletionSource<ObserverSubscriberResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            reactorMediator.OnNext(
                _observerId,
                connectedClient.ConnectionId,
                events,
                tcs);
            return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (TaskCanceledException taskCanceledException)
        {
            logger.OnNextException(taskCanceledException, _observerId, _eventStore, _namespace);
            return ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "Task was cancelled");
        }
        catch (TimeoutException timeoutException)
        {
            logger.OnNextException(timeoutException, _observerId, _eventStore, _namespace);
            return ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "Timeout");
        }
    }
}
