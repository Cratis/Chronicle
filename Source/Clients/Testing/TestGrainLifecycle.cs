// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Represents a test implementation of <see cref="IGrainLifecycle"/> that collects lifecycle
/// observers so that grains can be constructed outside an Orleans silo.
/// </summary>
internal sealed class TestGrainLifecycle : IGrainLifecycle
{
    readonly Collection<(int Stage, ILifecycleObserver Observer)> _observers = [];

    /// <inheritdoc/>
    public void AddMigrationParticipant(IGrainMigrationParticipant participant)
    {
    }

    /// <inheritdoc/>
    public void RemoveMigrationParticipant(IGrainMigrationParticipant participant)
    {
    }

    /// <inheritdoc/>
    public IDisposable Subscribe(string observerName, int stage, ILifecycleObserver observer)
    {
        var item = (Stage: stage, Observer: observer);
        _observers.Add(item);

        return new RemoveObserverDisposable(_observers, item);
    }

    /// <summary>
    /// Triggers the start phase for all registered lifecycle observers in stage order.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    internal Task TriggerStartAsync()
    {
        var tasks = _observers.OrderBy(x => x.Stage).Select(x => x.Observer.OnStart(CancellationToken.None));

        return Task.WhenAll(tasks.ToArray());
    }

    /// <summary>
    /// Triggers the stop phase for all registered lifecycle observers.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    internal Task TriggerStopAsync()
    {
        var tasks = _observers.Select(x => x.Observer.OnStop(CancellationToken.None));

        return Task.WhenAll(tasks.ToArray());
    }

    sealed class RemoveObserverDisposable(
        Collection<(int Stage, ILifecycleObserver Observer)> observers,
        (int Stage, ILifecycleObserver Observer) item) : IDisposable
    {
        public void Dispose() => observers.Remove(item);
    }
}
