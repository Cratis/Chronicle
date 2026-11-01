// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Linq;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.given;

/// <summary>
/// Sets up an <see cref="an_observer_state_storage"/> with one seeded observer, a short polling interval
/// and a subscription to <see cref="ObserverStateStorage.ObserveAll"/> that records every emission.
/// </summary>
public class an_observing_observer_state_storage : an_observer_state_storage
{
    protected static readonly ObserverId _observerId = new("test-observer");
    protected static readonly TimeSpan _pollingInterval = TimeSpan.FromMilliseconds(50);
    protected ConcurrentQueue<IEnumerable<KernelObserverState>> _emissions;
    protected SemaphoreSlim _emitted;
    protected IDisposable _subscription;
    readonly SemaphoreSlim _connectionGate = new(1, 1);
    readonly SemaphoreSlim _scopesReleased = new(0);

    async Task Establish()
    {
        _database.LiveQueryPollingInterval.Returns(_pollingInterval);
        _emissions = new();
        _emitted = new(0);

        // The fixture shares one SQLite connection, which cannot serve overlapping scopes, so serialize them
        // while the poller runs next to the spec body.
        _database.Namespace(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns(_ => CreateSerializedScope());

        await using var context = CreateContext();
        context.Observers.Add(new ObserverState { Id = _observerId, RunningState = ObserverRunningState.Active });
        await context.SaveChangesAsync();
    }

    protected async Task Subscribe()
    {
        var subject = _storage.ObserveAll();
        _subscription = await Task.Run(() => subject.Subscribe(states =>
        {
            _emissions.Enqueue(states);
            _emitted.Release();
        }));
        await WaitForEmission();
    }

    async Task<DbContextScope<NamespaceDbContext>> CreateSerializedScope()
    {
        await _connectionGate.WaitAsync();
        await _load.Task;
        return new DbContextScope<NamespaceDbContext>(CreateContext(), () =>
        {
            _connectionGate.Release();
            _scopesReleased.Release();
        });
    }

    protected Task WaitForEmission() => _emitted.WaitAsync(TimeSpan.FromSeconds(5));

    /// <summary>
    /// Waits until the given number of database scopes have been completed, each poll completing one scope.
    /// </summary>
    /// <param name="count">Number of completed scopes to wait for.</param>
    /// <returns>Awaitable task.</returns>
    protected async Task WaitForCompletedScopes(int count)
    {
        while (_scopesReleased.Wait(0))
        {
        }

        for (var i = 0; i < count; i++)
        {
            (await _scopesReleased.WaitAsync(TimeSpan.FromSeconds(5))).ShouldBeTrue();
        }
    }

    void Destroy() => _subscription?.Dispose();
}
