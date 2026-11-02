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
    readonly SemaphoreSlim _semaphore = new(
        Math.Max(1, options.Value.Observers.MaxConcurrentRegistrations),
        Math.Max(1, options.Value.Observers.MaxConcurrentRegistrations));

    /// <inheritdoc/>
    public async ValueTask<IDisposable> Enter(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        return Disposable.Create(() => _semaphore.Release());
    }

    /// <inheritdoc/>
    public void Dispose() => _semaphore.Dispose();
}
