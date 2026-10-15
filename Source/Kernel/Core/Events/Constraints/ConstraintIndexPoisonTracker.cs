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
    static readonly string[] _errorCodeProperties = ["Code", "Number", "SqlState"];
    readonly Dictionary<ConstraintName, FailureRun> _runs = [];
    readonly HashSet<(ConstraintName Name, EventSequenceNumber Position)> _settled = [];

    /// <summary>
    /// Gets settlements retained across replay replacement within this activation.
    /// </summary>
    internal IEnumerable<(ConstraintName Name, EventSequenceNumber Position)> Settled => _settled;

    /// <summary>
    /// Releases settlements only after their constraint has advanced past them.
    /// </summary>
    /// <param name="name">The repaired or removed constraint.</param>
    /// <param name="endPoint">The exclusive confirmed end point.</param>
    internal void ForgetSettledBefore(ConstraintName name, EventSequenceNumber endPoint) =>
        _settled.RemoveWhere(item => item.Name == name && item.Position < endPoint);

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
            var identity = FailureIdentity(failure.Error);
            var count = _runs.TryGetValue(name, out var previous) && previous.Position == position && previous.ErrorType == type && previous.Identity == identity ? previous.Count + 1 : 1;
            _runs[name] = new(position, type, identity, count);
            if (count < 5) continue;
            _settled.Add((name, position));
            poisoned.Add(name, failure);
            _runs.Remove(name);
        }
        return poisoned;
    }

    static string FailureIdentity(Exception error)
    {
        // Messages can contain changing request ids; they are not a stable failure identity.
        return ProviderFailureIdentity(error) ?? $"{error.GetType().FullName}:{error.HResult}";
    }

    static string? ProviderFailureIdentity(Exception error)
    {
        // Keep provider dependencies outside Core while recognizing codes beneath storage wrappers.
        foreach (var name in _errorCodeProperties)
        {
            if (error.GetType().GetProperty(name)?.GetValue(error) is { } code) return $"{error.GetType().FullName}:{name}:{code}";
        }
        if (error is AggregateException aggregate)
        {
            foreach (var inner in aggregate.InnerExceptions)
            {
                if (ProviderFailureIdentity(inner) is { } identity) return identity;
            }
            return null;
        }
        return error.InnerException is { } cause ? ProviderFailureIdentity(cause) : null;
    }

    sealed record FailureRun(EventSequenceNumber Position, Type ErrorType, string Identity, int Count);
}
