// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Services.Observation;

/// <summary>
/// Keeps the subscription of a connected client in place for as long as its observation stream is open.
/// </summary>
/// <remarks>
/// <para>
/// A subscription lives in grain activations only - the observer's and the client registration's. Either can be lost
/// while the client stays connected: with a silo that leaves the cluster, or when Orleans deactivates activations on a
/// surviving silo because the silo owning their directory entries left. The client never registers again in that case,
/// so the open stream is the only thing that outlives both activations and still knows the client is there.
/// </para>
/// <para>
/// A failed check is logged and retried at the next one; a silo that is still leaving the cluster can fail a call or two.
/// </para>
/// </remarks>
internal static class ClientSubscriptionKeeper
{
    /// <summary>
    /// Keep the subscription until the stream closes.
    /// </summary>
    /// <param name="keep">Keeps the subscription in place, restoring it if it was lost.</param>
    /// <param name="period">How often to check.</param>
    /// <param name="connectionId">The <see cref="ConnectionId"/> of the client.</param>
    /// <param name="logger">The <see cref="ILogger"/> to log with.</param>
    /// <param name="cancellationToken">Cancelled when the stream closes.</param>
    /// <returns>A task that completes when the stream has closed.</returns>
    public static async Task KeepWhileConnected(
        Func<Task> keep,
        TimeSpan period,
        ConnectionId connectionId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(period);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await Keep(keep, connectionId, logger, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // The stream closed; there is nothing left to keep.
        }
    }

    static async Task Keep(Func<Task> keep, ConnectionId connectionId, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            await keep();
        }
        catch (Exception ex)
        {
            // A stream that closed while checking needs nothing; its disconnect unsubscribes the client.
            if (!cancellationToken.IsCancellationRequested)
            {
                logger.KeepingClientSubscriptionFailed(connectionId, ex);
            }
        }
    }
}
