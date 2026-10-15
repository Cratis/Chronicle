// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Requires repeated event-local failures and successful storage work before settling poison input.
/// </summary>
/// <param name="classifier">The active provider's transient error policy.</param>
internal sealed class ConstraintIndexPoisonTracker(IStorageErrorClassifier classifier)
{
    readonly Dictionary<ConstraintName, FailureRun> _runs = [];

    /// <summary>
    /// Observes one repair attempt's per-constraint failures.
    /// </summary>
    /// <param name="failures">The first failure per constraint.</param>
    /// <param name="storageSucceeded">Whether another storage call succeeded in this attempt.</param>
    /// <returns>The event-local failures that have reached five consecutive identical attempts.</returns>
    internal IReadOnlyDictionary<ConstraintName, ConstraintIndexReplayFailure> Observe(IReadOnlyDictionary<ConstraintName, ConstraintIndexReplayFailure> failures, bool storageSucceeded)
    {
        foreach (var name in _runs.Keys.Except(failures.Keys).ToArray()) _runs.Remove(name);
        var poisoned = new Dictionary<ConstraintName, ConstraintIndexReplayFailure>();
        foreach (var (name, failure) in failures)
        {
            if (!storageSucceeded || failure.Position is not { } position || classifier.IsTransient(failure.Error))
            {
                _runs.Remove(name);
                continue;
            }
            var type = failure.Error.GetType();
            var count = _runs.TryGetValue(name, out var previous) && previous.Position == position && previous.ErrorType == type ? previous.Count + 1 : 1;
            _runs[name] = new(position, type, count);
            if (count < 5) continue;
            poisoned.Add(name, failure);
            _runs.Remove(name);
        }
        return poisoned;
    }

    sealed record FailureRun(EventSequenceNumber Position, Type ErrorType, int Count);
}
