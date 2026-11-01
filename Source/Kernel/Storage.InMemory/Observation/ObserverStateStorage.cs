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
    readonly ConcurrentDictionary<ObserverId, DateTimeOffset> _runningStatesChangedAt = new();
    readonly ReplaySubject<IEnumerable<ObserverState>> _allSubject = new(1);

    /// <summary>Serializes every mutation, so a timestamp write cannot interleave with a delete, rename or ownership change.</summary>
    readonly Lock _mutationLock = new();

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
    public Task<ObserverState> GetReadOnly(ObserverId observerId) => Get(observerId);

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
        lock (_mutationLock)
        {
            if (!_states.TryGetValue(state.Identifier, out var existing))
            {
                _states[state.Identifier] = state with { Owner = owner };
            }
            else
            {
                if (existing.Owner is not null && existing.Owner != owner) return Task.FromResult(false);
                _states[state.Identifier] = state with
                {
                    Owner = owner,
                    TypeSetControl = state.TypeSetControl is not null &&
                        (existing.TypeSetControl is null || state.TypeSetControl.Revision.Value >= existing.TypeSetControl.Revision.Value)
                            ? state.TypeSetControl
                            : existing.TypeSetControl,
                    Initialization = state.Initialization ?? existing.Initialization
                };
            }
        }

        _allSubject.OnNext(Snapshot());
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public Task ClaimOwnership(ObserverId observerId, EventSequenceId owner)
    {
        lock (_mutationLock)
        {
            if (!_states.TryGetValue(observerId, out var existing)) return Task.CompletedTask;
            _states[observerId] = existing with { Owner = owner };
        }

        _allSubject.OnNext(Snapshot());
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<DateTimeOffset?> GetRunningStateChangedAt(ObserverId observerId) =>
        Task.FromResult<DateTimeOffset?>(_runningStatesChangedAt.TryGetValue(observerId, out var changedAt) ? changedAt : null);

    /// <inheritdoc/>
    public Task<IReadOnlyDictionary<ObserverId, DateTimeOffset>> GetRunningStatesChangedAt() =>
        Task.FromResult<IReadOnlyDictionary<ObserverId, DateTimeOffset>>(new Dictionary<ObserverId, DateTimeOffset>(_runningStatesChangedAt));

    /// <inheritdoc/>
    public Task<bool> SaveRunningStateChangedAt(ObserverId observerId, DateTimeOffset? runningStateChangedAt, EventSequenceId owner)
    {
        lock (_mutationLock)
        {
            if (!_states.TryGetValue(observerId, out var existing) || (existing.Owner is not null && existing.Owner != owner))
            {
                return Task.FromResult(false);
            }

            if (runningStateChangedAt is null)
            {
                _runningStatesChangedAt.TryRemove(observerId, out _);
            }
            else
            {
                _runningStatesChangedAt[observerId] = runningStateChangedAt.Value;
            }
        }

        _allSubject.OnNext(Snapshot());
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public Task Delete(ObserverId observerId)
    {
        lock (_mutationLock)
        {
            if (!_states.TryRemove(observerId, out _)) return Task.CompletedTask;
            _runningStatesChangedAt.TryRemove(observerId, out _);
        }

        _allSubject.OnNext(Snapshot());
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Rename(ObserverId currentId, ObserverId newId)
    {
        lock (_mutationLock)
        {
            if (!_states.TryRemove(currentId, out var state)) return Task.CompletedTask;
            _states[newId] = state with { Identifier = newId };
            if (_runningStatesChangedAt.TryRemove(currentId, out var changedAt))
            {
                _runningStatesChangedAt[newId] = changedAt;
            }
        }

        _allSubject.OnNext(Snapshot());
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose() => _allSubject.Dispose();

    ObserverState[] Snapshot() => _states.Values.ToArray();
}
