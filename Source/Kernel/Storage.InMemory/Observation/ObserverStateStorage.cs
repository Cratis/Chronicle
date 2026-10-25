// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation;

/// <summary>
/// Represents an in-memory implementation of <see cref="IObserverStateStorage"/>.
/// </summary>
public sealed class ObserverStateStorage : IObserverStateStorage, IDisposable
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
        Task.FromResult(_states.TryGetValue(observerId, out var state) ? state : new ObserverState());

    /// <inheritdoc/>
    public Task<IEnumerable<ObserverState>> GetAll() => Task.FromResult<IEnumerable<ObserverState>>(Snapshot());

    /// <inheritdoc/>
    public Task<IEnumerable<ObserverId>> GetRetired(IEnumerable<ObserverId> observerIds) =>
        Task.FromResult<IEnumerable<ObserverId>>(observerIds.Distinct()
            .Where(id => _states.TryGetValue(id, out var state) && state.AlertDisposition == AlertDisposition.Retired)
            .ToArray());

    /// <inheritdoc/>
    public Task<bool> Save(ObserverState state, EventSequenceId owner)
    {
        while (true)
        {
            if (!_states.TryGetValue(state.Identifier, out var existing))
            {
                if (!_states.TryAdd(state.Identifier, state with { Owner = owner })) continue;
            }
            else
            {
                if (existing.Owner is not null && existing.Owner != owner) return Task.FromResult(false);
                var incoming = state with
                {
                    Owner = owner,
                    TypeSetControl = state.TypeSetControl is not null &&
                        (existing.TypeSetControl is null || state.TypeSetControl.Revision.Value >= existing.TypeSetControl.Revision.Value)
                            ? state.TypeSetControl
                            : existing.TypeSetControl,
                    Initialization = state.Initialization ?? existing.Initialization
                };
                if (!_states.TryUpdate(state.Identifier, incoming, existing)) continue;
            }

            _allSubject.OnNext(Snapshot());
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc/>
    public Task ClaimOwnership(ObserverId observerId, EventSequenceId owner)
    {
        while (_states.TryGetValue(observerId, out var existing))
        {
            if (!_states.TryUpdate(observerId, existing with { Owner = owner }, existing)) continue;
            _allSubject.OnNext(Snapshot());
            break;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Delete(ObserverId observerId)
    {
        if (_states.TryRemove(observerId, out _))
        {
            _allSubject.OnNext(Snapshot());
        }

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

    /// <inheritdoc/>
    public void Dispose() => _allSubject.Dispose();

    ObserverState[] Snapshot() => _states.Values.ToArray();
}
