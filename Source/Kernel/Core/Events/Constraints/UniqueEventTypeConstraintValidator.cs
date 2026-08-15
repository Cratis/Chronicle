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
        //
        // The whole definition goes to storage rather than only its covered event types, because the answer also
        // depends on the removal event: a covered event that precedes the most recent removal belongs to a closed
        // cycle and no longer blocks anything.
        var (isAllowed, sequenceNumber) = await storage.IsAllowed(definition, context.EventSourceId, scopeKey);

        // The event source id is deliberately absent from the message. A violation message is a response body -
        // it travels back to whoever attempted the append as a validation result - and an event source id is the
        // stream identity and the compliance subject, which Chronicle's own analyzers treat as potentially
        // sensitive and which cannot be encrypted because correlation depends on reading it. It is also the one
        // value the caller already holds: it just tried to append an event for that event source.
        //
        // It is not moved to the violation details either. Details travel the same route to the same caller, so
        // that would relocate the value rather than withhold it.
        return isAllowed ?
            ConstraintValidationResult.Success :
            new()
            {
                Violations =
                [
                    this.CreateViolation(
                        context,
                        sequenceNumber,
                        $"Event '{context.EventTypeId}' violated a unique event type constraint on sequence number {sequenceNumber}")
                ]
            };
    }
}
