// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Keeps the subscription a connected client registered with an observer in place for as long as the client is connected.
/// </summary>
/// <remarks>
/// <para>
/// An observer's subscription lives in its activation only. When the silo hosting an observer is lost, the observer
/// comes back on another silo without it, while the client stays connected to its own silo and never registers again.
/// The registration grain on the client's silo survives, so it is the one that notices and puts the client back.
/// </para>
/// <para>
/// The check runs as a non-interleaved timer of the registration grain, so it never races the grain's own
/// subscribe and unsubscribe. A healthy check is a single interleaved, read-only call to the observer.
/// </para>
/// </remarks>
/// <param name="grain">The registration grain owning the timer.</param>
/// <param name="period">How often to check.</param>
/// <param name="logger">The logger to use.</param>
internal sealed class ClientSubscriptionKeeper(IGrainBase grain, TimeSpan period, ILogger logger)
{
    IGrainTimer? _timer;

    /// <summary>
    /// Start keeping the subscription of a client, replacing anything kept before.
    /// </summary>
    /// <param name="observer">The <see cref="IObserver"/> the client subscribed.</param>
    /// <param name="connectionId">The <see cref="ConnectionId"/> of the client.</param>
    /// <param name="connectedClients">The <see cref="IConnectedClients"/> of the client's silo.</param>
    /// <param name="restore">Restores the subscription for the connected client.</param>
    public void Keep(IObserver observer, ConnectionId connectionId, IConnectedClients connectedClients, Func<ConnectedClient, Task> restore)
    {
        Stop();
        _timer = grain.RegisterGrainTimer(
            _ => Check(observer, connectionId, connectedClients, restore),
            new GrainTimerCreationOptions { DueTime = period, Period = period, Interleave = false, KeepAlive = true });
    }

    /// <summary>
    /// Stop keeping the subscription.
    /// </summary>
    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    async Task Check(IObserver observer, ConnectionId connectionId, IConnectedClients connectedClients, Func<ConnectedClient, Task> restore)
    {
        try
        {
            if (await observer.KnowsClient(connectionId)) return;

            // A client that has left is unsubscribed through its disconnect; there is nothing left to keep.
            if (!await connectedClients.IsConnected(connectionId))
            {
                Stop();
                return;
            }

            logger.RestoringLostClientSubscription(connectionId);
            await restore(await connectedClients.GetConnectedClient(connectionId));
        }
        catch (Exception ex)
        {
            // The next check retries; a silo that is still leaving the cluster can fail a call or two.
            logger.RestoringLostClientSubscriptionFailed(connectionId, ex);
        }
    }
}
