// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Verifies access to a connection without acquiring or renewing ownership.
/// </summary>
public static class ConnectionAccess
{
    /// <summary>
    /// Checks local ownership first, then the cluster reservation on a local miss.
    /// </summary>
    /// <param name="ownership">The receiving silo's ownership index.</param>
    /// <param name="grainFactory">The cluster grain factory.</param>
    /// <param name="connectionId">The connection identifier.</param>
    /// <param name="owner">The caller's identity.</param>
    /// <param name="timeout">The maximum time to wait for the cluster.</param>
    /// <param name="timeProvider">The clock for the timeout, or the system clock.</param>
    /// <param name="cancellationToken">The caller's cancellation token.</param>
    /// <returns>The access outcome. Local hits complete synchronously without allocating.</returns>
    public static ValueTask<ConnectionAccessOutcome> Verify(
        this IConnectionOwnership ownership,
        IGrainFactory grainFactory,
        string connectionId,
        ConnectionOwner owner,
        TimeSpan timeout,
        TimeProvider? timeProvider = null,
        CancellationToken cancellationToken = default)
    {
        if (ownership.IsOwnedBy(connectionId, owner)) return new(ConnectionAccessOutcome.Granted);
        if (string.IsNullOrEmpty(connectionId) || owner.IsUnidentified) return new(ConnectionAccessOutcome.Denied);
        return VerifyReservation(grainFactory, connectionId, owner, timeout, timeProvider ?? TimeProvider.System, cancellationToken);
    }

    static async ValueTask<ConnectionAccessOutcome> VerifyReservation(
        IGrainFactory grainFactory,
        string connectionId,
        ConnectionOwner owner,
        TimeSpan timeout,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        try
        {
            var held = await grainFactory.GetGrain<IConnectionReservation>(connectionId)
                .IsHeldBy(owner).WaitAsync(timeout, timeProvider, cancellationToken);
            return held ? ConnectionAccessOutcome.Granted : ConnectionAccessOutcome.Denied;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // Includes timeouts, unavailable activations and mixed-version method dispatch failures. Never claim here.
            return ConnectionAccessOutcome.Unverifiable;
        }
    }
}
