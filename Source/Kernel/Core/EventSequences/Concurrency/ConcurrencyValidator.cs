// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// Represents an implementation <see cref="IConcurrencyValidator"/>.
/// </summary>
/// <param name="eventSequenceStorage">The <see cref="IEventSequenceStorage"/>.</param>
/// <param name="logger">The logger.</param>
/// <param name="currentMutationEpoch">The <see cref="EventSequenceMutationEpoch"/> the event sequence is at right now.</param>
public class ConcurrencyValidator(
    IEventSequenceStorage eventSequenceStorage,
    ILogger<ConcurrencyValidator> logger,
    EventSequenceMutationEpoch currentMutationEpoch) : IConcurrencyValidator
{
    /// <inheritdoc/>
    public async ValueTask<Option<ConcurrencyViolation>> Validate(EventSourceId eventSourceId, ConcurrencyScope scope)
    {
        if (!scope.ShouldBeValidated)
        {
            LogIfIncomplete(eventSourceId, scope);
            return Option<ConcurrencyViolation>.None();
        }

        if (scope.MutationEpoch is { } expectedEpoch && (expectedEpoch.IsMutating || expectedEpoch != currentMutationEpoch))
        {
            logger.HistoryMutatedAfterRead(eventSourceId, expectedEpoch, currentMutationEpoch);
            return new ConcurrencyViolation(eventSourceId, scope.SequenceNumber, scope.SequenceNumber);
        }

        var tailSequenceNumber = await eventSequenceStorage.GetTailSequenceNumber(
            scope.EventTypes,
            scope.EventSourceId ? eventSourceId : null,
            scope.EventSourceType,
            scope.EventStreamId,
            scope.EventStreamType);

        if (!tailSequenceNumber.IsActualValue)
        {
            return Option<ConcurrencyViolation>.None();
        }

        var violated = scope.ExpectsNoMatchingEvent || !(tailSequenceNumber <= scope.SequenceNumber);
        return !violated
            ? Option<ConcurrencyViolation>.None()
            : await WithConflictingEvent(new ConcurrencyViolation(eventSourceId, scope.SequenceNumber, tailSequenceNumber), scope);
    }

    /// <inheritdoc/>
    public async ValueTask<IEnumerable<ConcurrencyViolation>> Validate(ConcurrencyScopes scopes)
    {
        if (scopes.Scopes.All(_ => !_.Value.ShouldBeValidated))
        {
            foreach (var (eventSourceId, scope) in scopes.Scopes)
            {
                LogIfIncomplete(eventSourceId, scope);
            }

            return [];
        }

        var validationTasks = scopes.Scopes.Select(async eventSourceIdAndScope =>
        {
            var (eventSourceId, scope) = eventSourceIdAndScope;
            return await Validate(eventSourceId, scope);
        });
        var validations = await Task.WhenAll(validationTasks);
        var violations = validations.Where(validation => validation.IsT0).Select(validation => validation.AsT0);
        return violations.ToArray();
    }

    /// <inheritdoc/>
    public async ValueTask<IEnumerable<ConcurrencyViolation>> Validate(ConcurrencyScopeConjunctions scopes)
    {
        var violations = new List<ConcurrencyViolation>();
        foreach (var (eventSourceId, scope, index) in scopes.Entries)
        {
            var result = await Validate(eventSourceId, scope);
            if (result.TryGetValue(out var violation))
            {
                violations.Add(violation with
                {
                    ScopeIndex = index,
                    Scope = Sequences.ConcurrencyScopeConverters.ToApi(scope)
                });
            }
        }

        return violations;
    }

    /// <summary>
    /// Adds the first event that matched the scope after the expected sequence number to a violation.
    /// </summary>
    /// <param name="violation">The <see cref="ConcurrencyViolation"/> to add it to.</param>
    /// <param name="scope">The violated <see cref="ConcurrencyScope"/>.</param>
    /// <returns>The violation, with the conflicting event when it could be read.</returns>
    /// <remarks>
    /// This runs only once a violation is certain, so the read costs nothing on the success path. The violation is
    /// what refuses the append; failing to read the event that caused it must not turn a refusal into an error.
    /// </remarks>
    async Task<ConcurrencyViolation> WithConflictingEvent(ConcurrencyViolation violation, ConcurrencyScope scope)
    {
        try
        {
            var from = scope.ExpectsNoMatchingEvent ? EventSequenceNumber.First : scope.SequenceNumber.Next();
            using var cursor = await eventSequenceStorage.GetFromSequenceNumber(
                from,
                scope.EventSourceId ? violation.EventSourceId : null,
                scope.EventSourceType,
                scope.EventStreamType,
                scope.EventStreamId,
                scope.EventTypes);
            var first = await cursor.MoveNext() ? cursor.Current.FirstOrDefault() : null;
            return first is null
                ? violation
                : violation with { ConflictingEventTypeId = first.Context.EventType.Id, ConflictingSequenceNumber = first.Context.SequenceNumber };
        }
        catch (Exception exception)
        {
            logger.CouldNotReadConflictingEvent(violation.EventSourceId, exception);
            return violation;
        }
    }

    /// <summary>
    /// Report a scope that asked for a concurrency check without saying what it expects.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> the append is for.</param>
    /// <param name="scope">The <see cref="ConcurrencyScope"/> that is being skipped.</param>
    /// <remarks>
    /// The signal a caller acts on is the append result, which reports the check as not performed - this log line
    /// is a diagnostic aid on top of it. It is Debug rather than Warning because the state is the shipped default,
    /// not an anomaly: with checking the first append into a scope not opted into, the strategy resolves an empty
    /// narrowing to a scope with no expectation, so an application creating many event sources produces this line
    /// on every one of them. A caller that did opt in sends the expectation that no matching event exists, which
    /// is validated rather than skipped - so a scope reaching here carries no sign that a check was wanted, and
    /// there is nothing to warn about.
    /// </remarks>
    void LogIfIncomplete(EventSourceId eventSourceId, ConcurrencyScope scope)
    {
        if (scope.IsIncomplete)
        {
            logger.SkippingIncompleteConcurrencyScope(eventSourceId);
        }
    }
}
