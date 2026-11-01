// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation;

/// <summary>
/// Caches running-state timestamps for one subscription, confirming transitions with bounded refreshes.
/// </summary>
/// <param name="storage">The observer state storage.</param>
/// <remarks>Not thread-safe: callers must serialize snapshots.</remarks>
internal sealed class RunningStatesChangedAtCache(IObserverStateStorage storage)
{
    /// <summary>
    /// The maximum number of follow-up refreshes for an unconfirmed transition.
    /// </summary>
    internal const int FollowUpRefreshes = 3;

    readonly Dictionary<ObserverId, (DateTimeOffset? Before, int Remaining)> _unconfirmed = [];
    Dictionary<ObserverId, ObserverRunningState>? _runningStates;
    IReadOnlyDictionary<ObserverId, DateTimeOffset> _changedAt = new Dictionary<ObserverId, DateTimeOffset>();

    /// <summary>
    /// Gets timestamps for a snapshot, refreshing only initially or for unconfirmed transitions.
    /// </summary>
    /// <param name="states">The current observer snapshot.</param>
    /// <returns>The timestamp dictionary, replaced rather than mutated on refresh.</returns>
    public async Task<IReadOnlyDictionary<ObserverId, DateTimeOffset>> GetFor(IReadOnlyCollection<ObserverState> states)
    {
        var previous = _runningStates;
        _runningStates = [];
        foreach (var state in states)
        {
            _runningStates[state.Identifier] = state.RunningState;
        }

        if (previous is not null)
        {
            foreach (var (id, runningState) in _runningStates)
            {
                if (runningState != ObserverRunningState.Unknown && (!previous.TryGetValue(id, out var was) || was != runningState))
                {
                    _unconfirmed[id] = (_changedAt.TryGetValue(id, out var timestamp) ? timestamp : null, FollowUpRefreshes + 1);
                }
            }
            foreach (var id in _unconfirmed.Keys.Where(id => !_runningStates.ContainsKey(id)).ToArray())
            {
                _unconfirmed.Remove(id);
            }
            if (_unconfirmed.Count == 0) return _changedAt;
        }

        _changedAt = await storage.GetRunningStatesChangedAt();
        foreach (var (id, (before, remaining)) in _unconfirmed.ToArray())
        {
            DateTimeOffset? current = _changedAt.TryGetValue(id, out var timestamp) ? timestamp : null;
            if (current != before || remaining <= 1)
            {
                _unconfirmed.Remove(id);
            }
            else
            {
                _unconfirmed[id] = (before, remaining - 1);
            }
        }

        return _changedAt;
    }
}
