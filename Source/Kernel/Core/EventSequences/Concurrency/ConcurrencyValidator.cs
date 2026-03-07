// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.Concurrency;

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
}
