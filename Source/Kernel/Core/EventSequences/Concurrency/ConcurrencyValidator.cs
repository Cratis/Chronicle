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
            WarnIfIncomplete(eventSourceId, scope);
            return Option<ConcurrencyViolation>.None();
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
                WarnIfIncomplete(eventSourceId, scope);
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
    /// Failing open here looks identical to never having asked for a check, so a caller that believes its writes
    /// are serialized never finds out otherwise. The append is still allowed - rejecting it would break every
    /// caller that already builds such a scope - but it does not pass unnoticed.
    /// </remarks>
    void WarnIfIncomplete(EventSourceId eventSourceId, ConcurrencyScope scope)
    {
        if (scope.IsIncomplete)
        {
            logger.SkippingIncompleteConcurrencyScope(eventSourceId);
        }
    }
}
