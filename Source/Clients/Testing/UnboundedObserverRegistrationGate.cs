// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelGrpc;

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Represents an observer registration gate that never limits how many registrations are in flight.
/// </summary>
internal sealed class UnboundedObserverRegistrationGate : KernelGrpc::Cratis.Chronicle.Services.Observation.IObserverRegistrationGate
{
    /// <inheritdoc/>
    public ValueTask<IDisposable> Enter(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(System.Reactive.Disposables.Disposable.Empty);
    }
}
