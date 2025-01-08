// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Configuration;
using Cratis.Chronicle.Concepts.Events;
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
/// <param name="configurationProvider"><see cref="IConfigurationForObserverProvider"/> for providing <see cref="Observers"/> config.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
[ConnectedObserverPlacement]
public class ReactorObserverSubscriber(
    IReactorMediator reactorMediator,
    IConfigurationForObserverProvider configurationProvider,
    ILogger<ReactorObserverSubscriber> logger) : Grain, IReactorObserverSubscriber
{
    ObserverKey _key = ObserverKey.NotSet;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var subscriberKey = ObserverSubscriberKey.Parse(this.GetPrimaryKeyString());
        _key = new(subscriberKey.ObserverId, subscriberKey.EventStore, subscriberKey.Namespace, subscriberKey.EventSequenceId);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<ObserverSubscriberResult> OnNext(IEnumerable<AppendedEvent> events, ObserverSubscriberContext context)
    {
        foreach (var @event in events)
        {
            logger.EventReceived(
                _key.ObserverId,
                _key.EventStore,
                _key.Namespace,
                @event.Metadata.Type.Id,
                _key.EventSequenceId,
                @event.Context.SequenceNumber);
        }

        if (context.Metadata is not ConnectedClient connectedClient)
        {
            throw new MissingStateForReactorSubscriber(_key.ObserverId);
        }
        var tcs = new TaskCompletionSource<ObserverSubscriberResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            var timeout = await configurationProvider.GetSubscriberTimeoutForObserver(_key);
            reactorMediator.OnNext(_key.ObserverId, connectedClient.ConnectionId, events, tcs);
            return await tcs.Task.WaitAsync(timeout);
        }
        catch (TaskCanceledException taskCanceledException)
        {
            logger.OnNextException(taskCanceledException, _key.ObserverId, _key.EventStore, _key.Namespace);
            return ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "Task was cancelled");
        }
        catch (TimeoutException timeoutException)
        {
            logger.OnNextException(timeoutException, _key.ObserverId, _key.EventStore, _key.Namespace);
            return ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "Timeout");
        }
    }
}
