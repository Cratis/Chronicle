// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation;

/// <summary>
/// Holds a permit from the <see cref="IObserverRegistrationGate"/> that can be given back and taken again while a
/// registration waits between attempts.
/// </summary>
/// <remarks>
/// A registration that waits for a busy observer must not keep a gate slot while it does: with
/// <c language="text">observers.maxConcurrentRegistrations</c> set, one wedged observer would otherwise block healthy
/// registrations for every attempt it makes. Re-entering gives the slot back first and then queues behind whoever is
/// already waiting.
/// </remarks>
/// <param name="gate">The <see cref="IObserverRegistrationGate"/> the permit comes from.</param>
internal sealed class ObserverRegistrationGateLease(IObserverRegistrationGate gate) : IDisposable
{
    IDisposable? _permit;

    /// <inheritdoc/>
    public void Dispose()
    {
        _permit?.Dispose();
        _permit = null;
    }

    /// <summary>
    /// Waits for a permit from the gate.
    /// </summary>
    /// <param name="gate">The <see cref="IObserverRegistrationGate"/> to enter.</param>
    /// <param name="cancellationToken">Cancelled when the registering client disconnects.</param>
    /// <returns>The lease holding the permit.</returns>
    internal static async Task<ObserverRegistrationGateLease> Enter(IObserverRegistrationGate gate, CancellationToken cancellationToken)
    {
        var lease = new ObserverRegistrationGateLease(gate);
        await lease.Reenter(cancellationToken);
        return lease;
    }

    /// <summary>
    /// Gives the held permit back, then waits for a new one.
    /// </summary>
    /// <param name="cancellationToken">Cancelled when the registering client disconnects.</param>
    /// <returns>Awaitable task.</returns>
    internal async Task Reenter(CancellationToken cancellationToken)
    {
        Dispose();
        _permit = await gate.Enter(cancellationToken);
    }
}
