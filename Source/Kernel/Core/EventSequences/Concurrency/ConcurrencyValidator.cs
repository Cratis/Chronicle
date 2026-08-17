// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
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
public class ConcurrencyValidator(IEventSequenceStorage eventSequenceStorage, ILogger<ConcurrencyValidator> logger) : IConcurrencyValidator
{
    /// <inheritdoc/>
    public async ValueTask<Option<ConcurrencyViolation>> Validate(EventSourceId eventSourceId, ConcurrencyScope scope)
    {
        if (!scope.ShouldBeValidated)
        {
            LogIfIncomplete(eventSourceId, scope);
            return Option<ConcurrencyViolation>.None();
        }

        var tailSequenceNumber = await eventSequenceStorage.GetTailSequenceNumber(
            scope.EventTypes,
            scope.EventSourceId ? eventSourceId : null,
            scope.EventSourceType,
            scope.EventStreamId,
            scope.EventStreamType);

        if (scope.ExpectsNoMatchingEvent)
        {
            return tailSequenceNumber.IsActualValue
                ? new ConcurrencyViolation(eventSourceId, scope.SequenceNumber, tailSequenceNumber)
                : Option<ConcurrencyViolation>.None();
        }

        if (!tailSequenceNumber.IsActualValue)
        {
            return Option<ConcurrencyViolation>.None();
        }

        var violated = !(tailSequenceNumber <= scope.SequenceNumber);
        return !violated
            ? Option<ConcurrencyViolation>.None()
            : new ConcurrencyViolation(eventSourceId, scope.SequenceNumber, tailSequenceNumber);
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
