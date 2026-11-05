// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Validates event-driven scope values and maintains owned closing and reopening transitions.
/// </summary>
/// <param name="definition">The closing declaration.</param>
/// <param name="storage">The closure storage.</param>
/// <param name="eventSequenceId">The event sequence the validator runs for.</param>
public class ClosesStreamConstraintValidator(ClosesStreamConstraintDefinition definition, IClosedStreamsConstraintStorage storage, EventSequenceId eventSequenceId) : IConstraintValidator, IHaveUpdateConstraintIndex, IObserveConstraintBatchEvents
{
    /// <inheritdoc/>
    public IConstraintDefinition Definition => definition;

    /// <inheritdoc/>
    public bool CanValidate(ConstraintValidationContext context) =>
        definition.EventTypeIds.Contains(context.EventTypeId) || definition.ReopenedBy.Contains(context.EventTypeId);

    /// <inheritdoc/>
    public Task<ConstraintValidationResult> Validate(ConstraintValidationContext context)
    {
        if (definition.TryResolveScope(context, out var scope))
        {
            return Task.FromResult(FitsTheStoredKey(context, scope) ? ConstraintValidationResult.Success : TooLong(context));
        }

        var message = definition.EventStreamIdFrom is null
            ? "A closing stream scope must have every participating dimension set"
            : $"Closing stream property '{definition.EventStreamIdFrom}' must have a non-empty value";
        if (definition.Name.Value.Length == 0) message = "A closing constraint must have a non-empty owner name";
        if (scope.IsDefaultStreamOnly) message = "The default stream cannot be closed or reopened by a closing constraint";

        return Task.FromResult(new ConstraintValidationResult
        {
            Violations = [this.CreateViolation(context, EventSequenceNumber.Unavailable, message, new() { { WellKnownConstraintDetailKeys.PropertyName, definition.EventStreamIdFrom ?? string.Empty } })]
        });
    }

    /// <inheritdoc/>
    public IUpdateConstraintIndex GetUpdateFor(ConstraintValidationContext context) => new ClosesStreamIndexUpdater(definition, context, storage);

    /// <inheritdoc/>
    void IObserveConstraintBatchEvents.RecordBatchEvent(ConstraintValidationContext context)
    {
        if (!CanValidate(context) || context.BatchClaims is null || !definition.TryResolveScope(context, out var scope)) return;
        if (definition.ReopenedBy.Contains(context.EventTypeId))
        {
            context.BatchClaims.RecordReopen(definition.Name.Value, scope);
        }
        else
        {
            context.BatchClaims.RecordClosure(new(scope, definition.Name.Value, EventSequenceNumber.Unavailable, null));
        }
    }

    bool FitsTheStoredKey(ConstraintValidationContext context, ClosedStreamScope scope) =>
        !definition.EventTypeIds.Contains(context.EventTypeId) ||
        ClosedStreamKeyLength.Fits(eventSequenceId, scope.Normalized(), definition.Name.Value);

    ConstraintValidationResult TooLong(ConstraintValidationContext context) => new()
    {
        Violations = [this.CreateViolation(
            context,
            EventSequenceNumber.Unavailable,
            $"The closing stream scope is too long to be stored: its identifiers may total at most {ClosedStreamKeyLength.MaxTotalLength} characters and each at most {ClosedStreamKeyLength.MaxColumnLength}",
            new() { { WellKnownConstraintDetailKeys.PropertyName, definition.EventStreamIdFrom ?? string.Empty } })]
    };
}
