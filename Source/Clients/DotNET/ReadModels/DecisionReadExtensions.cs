// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.ReadModels;

/// <summary>Additive entry points for guarded appends and non-DI reads.</summary>
public static class DecisionReadExtensions
{
    /// <summary>Gets a decision reader for this event store.</summary>
    /// <param name="eventStore">The event store.</param>
    /// <returns>A decision reader.</returns>
    public static IDecisionReads GetDecisionReads(this IEventStore eventStore) => DecisionReads.For(eventStore);

    /// <summary>Maps guarded append concurrency violations to decision conflicts.</summary>
    /// <param name="result">The guarded append result.</param>
    /// <param name="reads">The reads that guarded the append.</param>
    /// <returns>The read models and keys whose decisions conflicted, with the first conflicting event when the kernel reported it.</returns>
    public static IEnumerable<DecisionConflict> GetDecisionConflicts(this AppendManyResult result, IEnumerable<IDecisionRead> reads)
    {
        var violations = result.ConcurrencyViolations
            .GroupBy(_ => _.EventSourceId.Value, StringComparer.Ordinal)
            .ToDictionary(_ => _.Key, _ => _.First(), StringComparer.Ordinal);
        return reads
            .Select(read => (read, label: read.EventStreamType is not null || read.EventStreamId is not null ? read.Scope.EventSourceId!.Value : read.Key.Value))
            .Where(_ => violations.ContainsKey(_.label))
            .Select(_ => new DecisionConflict(_.read.ReadModelType, _.read.Key).CausedBy(violations[_.label]))
            .Distinct().ToArray();
    }

    /// <summary>Validates the reads and appends the events atomically in the event-log grain.</summary>
    /// <param name="sequence">The event-log sequence.</param>
    /// <param name="events">The events to append.</param>
    /// <param name="guardedBy">The protected reads to validate.</param>
    /// <returns>The append result.</returns>
    /// <exception cref="DecisionReadTargetMismatch">The sequence or a token targets a different log.</exception>
    /// <exception cref="UnprotectedDecisionRead">A read has no guard.</exception>
    /// <exception cref="ImmediateAppendRefused">Immediate appends are refused in the current flow.</exception>
    /// <exception cref="DecisionReadValidateOnlyNotSupported">The kernel rejected a validate-only append.</exception>
    /// <exception cref="ArgumentException">At least one guard is required.</exception>
    public static async Task<AppendManyResult> AppendMany(
        this IEventSequence sequence,
        IEnumerable<EventForEventSourceId> events,
        IEnumerable<IDecisionRead> guardedBy)
    {
        ImmediateAppendGuard.Append(nameof(AppendMany));
        var reads = guardedBy.ToArray();
        if (sequence.Id != EventSequenceId.Log)
        {
            throw new DecisionReadTargetMismatch();
        }
        if (reads.Length == 0)
        {
            throw new ArgumentException("At least one decision read is required.", nameof(guardedBy));
        }
        var first = reads[0];
        var scopes = DecisionReadScopes.Collect(reads, first.EventStore, first.Namespace);

        // A sequence obtained from another store could have the same ID. Only a concrete event store's
        // event log has an independently verifiable target identity.
        if (sequence is not EventSequence concrete || !concrete.MatchesTarget(first.EventStore, first.Namespace))
        {
            throw new DecisionReadTargetMismatch();
        }
        var batch = events.ToArray();
        scopes = DecisionReadScopes.ForEvents(scopes, batch);
        try
        {
            return await sequence.AppendMany(batch, concurrencyScopes: scopes);
        }
        catch (CommandFailed exception) when (
            batch.Length == 0 &&
            exception.Result?.ValidationResults.Any(_ => _.Message == "At least one event is required.") == true)
        {
            throw new DecisionReadValidateOnlyNotSupported();
        }
    }
}
