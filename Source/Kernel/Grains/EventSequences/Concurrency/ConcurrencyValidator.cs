// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Grains.EventSequences.Concurrency;

/// <summary>
/// Represents an implementation <see cref="IConcurrencyValidator"/>.
/// </summary>
/// <param name="eventSequenceStorage">The <see cref="IEventSequenceStorage"/>.</param>
public class ConcurrencyValidator(IEventSequenceStorage eventSequenceStorage) : IConcurrencyValidator
{
    /// <inheritdoc/>
    public async ValueTask<Option<ConcurrencyViolation>> Validate(EventSourceId eventSourceId, ConcurrencyScope scope)
    {
        if (!scope.ShouldBeValidated)
        {
            return Option<ConcurrencyViolation>.None();
        }

        var tailSequenceNumber = await eventSequenceStorage.GetTailSequenceNumber(
            scope.EventTypes,
            scope.EventSourceId ? eventSourceId : null,
            scope.EventSourceType,
            scope.EventStreamId,
            scope.EventStreamType);

        return tailSequenceNumber <= scope.SequenceNumber
            ? Option<ConcurrencyViolation>.None()
            : new ConcurrencyViolation(scope.SequenceNumber, tailSequenceNumber);
    }

    /// <inheritdoc/>
    public async ValueTask<ConcurrencyViolations> Validate(ConcurrencyScopes scopes)
    {
        if (scopes.All(_ => !_.Value.ShouldBeValidated))
        {
            return ConcurrencyViolations.None;
        }

        var validationTasks = scopes.Select(async eventSourceIdAndScope =>
        {
            var (eventSourceId, scope) = eventSourceIdAndScope;
            return (EventSourceId: eventSourceId, Result: await Validate(eventSourceId, scope));
        });
        var validations = await Task.WhenAll(validationTasks);
        var violations = validations.Where(validation => validation.Result.HasValue);
        return new ConcurrencyViolations(violations.ToDictionary(kvp => kvp.EventSourceId, kvp => kvp.Result.AsT0));
    }
}
