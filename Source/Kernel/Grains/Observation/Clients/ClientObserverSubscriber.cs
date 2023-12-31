// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Connections;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Grains.Observation.Placement;
using Aksio.Cratis.Observation;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Observation.Clients;

/// <summary>
/// Represents an implementation of <see cref="IClientObserverSubscriber"/>.
/// </summary>
/// <remarks>
/// We want this grain to be local to its activation. When a client connects, the service instance that
/// receives the connection will activate this grain and we then want it to be local to that service instance
/// and not perform a network hop. The <see cref="ObserverKey"/> contains a <see cref="ConnectionId"/> which
/// will make the observer unique per connection, helping us to achieve this.
/// </remarks>
[ConnectedObserverPlacement]
public class ClientObserverSubscriber : Grain, IClientObserverSubscriber
{
    readonly IObserverMediator _observerMediator;
    readonly ILogger<ClientObserverSubscriber> _logger;
    MicroserviceId _microserviceId = MicroserviceId.Unspecified;
    ObserverId _observerId = ObserverId.Unspecified;
    TenantId _tenantId = TenantId.NotSet;
    EventSequenceId _eventSequenceId = EventSequenceId.Unspecified;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientObserverSubscriber"/> class.
    /// </summary>
    /// <param name="observerMediator"><see cref="IObserverMediator"/> for notifying actual clients.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public ClientObserverSubscriber(
        IObserverMediator observerMediator,
        ILogger<ClientObserverSubscriber> logger)
    {
        _observerMediator = observerMediator;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var id = this.GetPrimaryKey(out var keyAsString);
        var key = ObserverSubscriberKey.Parse(keyAsString);
        _microserviceId = key.MicroserviceId;
        _observerId = id;
        _tenantId = key.TenantId;
        _eventSequenceId = key.EventSequenceId;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<ObserverSubscriberResult> OnNext(IEnumerable<AppendedEvent> events, ObserverSubscriberContext context)
    {
        foreach (var @event in events)
        {
            _logger.EventReceived(
                _observerId,
                _microserviceId,
                _tenantId,
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
            _observerMediator.OnNext(
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
