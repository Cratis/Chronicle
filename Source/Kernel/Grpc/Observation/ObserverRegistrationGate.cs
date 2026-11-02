// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Services.Observation;

/// <summary>
/// Shares a bounded pool of registration permits between gRPC reactors and reducers on a silo.
/// </summary>
/// <param name="options">The <see cref="ChronicleOptions"/>.</param>
public sealed class ObserverRegistrationGate(IOptions<ChronicleOptions> options) : IObserverRegistrationGate, IDisposable
{
    readonly SemaphoreSlim? _semaphore = options.Value.Observers.MaxConcurrentRegistrations > 0
        ? new(options.Value.Observers.MaxConcurrentRegistrations, options.Value.Observers.MaxConcurrentRegistrations)
        : null;

    /// <inheritdoc/>
    public ValueTask<IDisposable> Enter(CancellationToken cancellationToken) =>
        _semaphore is null ? new(Disposable.Empty) : WaitForPermit(_semaphore, cancellationToken);

    /// <inheritdoc/>
    public void Dispose() => _semaphore?.Dispose();

    static async ValueTask<IDisposable> WaitForPermit(SemaphoreSlim semaphore, CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);
        return Disposable.Create(() => semaphore.Release());
    }
}
