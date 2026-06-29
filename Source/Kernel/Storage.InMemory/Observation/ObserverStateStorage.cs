// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation;

/// <summary>
/// Represents an in-memory implementation of <see cref="IObserverStateStorage"/>.
/// </summary>
public sealed class ObserverStateStorage : IObserverStateStorage
{
    readonly ConcurrentDictionary<ObserverId, ObserverState> _states = new();
    readonly ReplaySubject<IEnumerable<ObserverState>> _allSubject = new(1);

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverStateStorage"/> class.
    /// </summary>
    public ObserverStateStorage() => _allSubject.OnNext(Snapshot());

    /// <inheritdoc/>
    public ISubject<IEnumerable<ObserverState>> ObserveAll() => _allSubject;

    /// <inheritdoc/>
    public Task<ObserverState> Get(ObserverId observerId) =>
        Task.FromResult(_states.TryGetValue(observerId, out var state) ? state : ObserverState.Empty);

    /// <inheritdoc/>
    public Task<IEnumerable<ObserverState>> GetAll() => Task.FromResult(Snapshot());

    /// <inheritdoc/>
    public Task Save(ObserverState state)
    {
        _states[state.Identifier] = state;
        _allSubject.OnNext(Snapshot());
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Rename(ObserverId currentId, ObserverId newId)
    {
        if (_states.TryRemove(currentId, out var state))
        {
            _states[newId] = state with { Identifier = newId };
            _allSubject.OnNext(Snapshot());
        }

        return Task.CompletedTask;
    }

    IEnumerable<ObserverState> Snapshot() => _states.Values.ToArray();
}
