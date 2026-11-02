// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Represents an implementation of <see cref="IClientDepartureNotifier"/>.
/// </summary>
/// <remarks>
/// Each observer is told through <see cref="IObserver.UnsubscribeIfMatchesClient"/>, which only acts when the connection
/// is one the observer knows, so a notification that arrives late or twice does no harm.
/// </remarks>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for getting observers.</param>
/// <param name="logger">The <see cref="ILogger"/> for logging.</param>
[Singleton]
public class ClientDepartureNotifier(IGrainFactory grainFactory, ILogger<ClientDepartureNotifier> logger) : IClientDepartureNotifier
{
    /// <summary>
    /// The most observers notified at the same time, so a connection that served many observers cannot flood the cluster.
    /// </summary>
    internal const int MaxConcurrentNotifications = 16;

    /// <inheritdoc/>
    public Task Notify(ConnectionId connectionId, IReadOnlyCollection<ObserverKey> observers) =>
        Parallel.ForEachAsync(
            observers,
            new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrentNotifications },
            (observer, _) => new ValueTask(NotifyObserver(connectionId, observer)));

    async Task NotifyObserver(ConnectionId connectionId, ObserverKey observer)
    {
        try
        {
            await grainFactory.GetGrain<IObserver>(observer).UnsubscribeIfMatchesClient(connectionId);
        }
        catch (Exception ex)
        {
            logger.FailedToNotifyObserver(connectionId, observer.ObserverId, ex);
        }
    }
}
