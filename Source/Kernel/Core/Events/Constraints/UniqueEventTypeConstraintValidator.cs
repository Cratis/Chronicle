// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents an implementation of <see cref="IConstraintValidator"/> for unique event type constraints.
/// </summary>
/// <param name="definition">The <see cref="UniqueEventTypeConstraintDefinition"/> to validate.</param>
/// <param name="storage">The <see cref="IUniqueEventTypesConstraintsStorage"/> to use.</param>
public class UniqueEventTypeConstraintValidator(
    UniqueEventTypeConstraintDefinition definition,
    IUniqueEventTypesConstraintsStorage storage) : IConstraintValidator
{
    /// <inheritdoc/>
    public IConstraintDefinition Definition => definition;

    /// <inheritdoc/>
    public bool CanValidate(ConstraintValidationContext context) => definition.EventTypeIds.Contains(context.EventTypeId);

    /// <inheritdoc/>
    public async Task<ConstraintValidationResult> Validate(ConstraintValidationContext context)
    {
        var scopeKey = definition.Scope.BuildScopeKey(context.EventSourceType, context.EventStreamType, context.EventStreamId);

        // Every covered event type is checked, not just the one being appended: the constraint allows at most
        // one event from the set, so an event source that already has a sibling type blocks this one too.
        var (isAllowed, sequenceNumber) = await storage.IsAllowed(definition.EventTypeIds, context.EventSourceId, scopeKey);
        return isAllowed ?
            ConstraintValidationResult.Success :
            new()
            {
                Violations =
                [
                    this.CreateViolation(
                        context,
                        sequenceNumber,
                        $"Event '{context.EventTypeId}' with event source id '{context.EventSourceId}' violated a unique event type constraint on sequence number {sequenceNumber}")
                ]
            };
    }
}
