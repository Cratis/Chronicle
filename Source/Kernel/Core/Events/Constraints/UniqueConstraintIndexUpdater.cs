// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents an implementation of <see cref="IUpdateConstraintIndex"/> for unique constraints.
/// </summary>
/// <param name="definition"><see cref="UniqueConstraintDefinition"/> for the updater to use.</param>
/// <param name="context">In which <see cref="ConstraintValidationContext"/> it is working with.</param>
/// <param name="storage"><see cref="IUniqueConstraintsStorage"/> to use for storage.</param>
public class UniqueConstraintIndexUpdater(
    UniqueConstraintDefinition definition,
    ConstraintValidationContext context,
    IUniqueConstraintsStorage storage) : IUpdateConstraintIndex
{
    /// <inheritdoc/>
    public Task Update(EventSequenceNumber eventSequenceNumber) => Update(eventSequenceNumber, CancellationToken.None);

    /// <inheritdoc/>
    public async Task Update(EventSequenceNumber eventSequenceNumber, CancellationToken cancellationToken)
    {
        if (Describe(eventSequenceNumber) is { } change)
        {
            await UniqueConstraintIndexChanges.Apply(storage, definition, change, cancellationToken);
        }
    }

    /// <summary>
    /// Describes the change the event makes to the index, without making it.
    /// </summary>
    /// <param name="eventSequenceNumber">The <see cref="EventSequenceNumber"/> of the event.</param>
    /// <returns>The <see cref="UniqueConstraintIndexChange"/>, or null when the event leaves the index as it is.</returns>
    internal UniqueConstraintIndexChange? Describe(EventSequenceNumber eventSequenceNumber)
    {
        var scopeKey = definition.Scope.BuildScopeKey(context.EventSourceType, context.EventStreamType, context.EventStreamId);

        // Any of the declared removal events releases the claim on its own — a lifecycle that ends in more than
        // one way has more than one terminal fact, and each of them frees the value.
        if (definition.RemovedWith.Contains(context.EventTypeId))
        {
            var removal = definition.RemovalEventDefinitions.SingleOrDefault(_ => _.EventTypeId == context.EventTypeId);
            if (definition.Mode == UniqueConstraintMode.PerValue && removal?.Properties.Any() == true)
            {
                var properties = removal.GetPropertiesAndValues(context).ToList();
                return properties.Count > 0
                    ? new(UniqueConstraintIndexChangeKind.RemoveValue, context.EventSourceId, eventSequenceNumber, scopeKey, properties.GetValue(definition.IgnoreCasing))
                    : null;
            }

            return new(UniqueConstraintIndexChangeKind.Remove, context.EventSourceId, eventSequenceNumber, scopeKey);
        }

        if (!definition.SupportsEventType(context.EventTypeId))
        {
            return null;
        }

        // The same guard the validator applies: properties that carry no value are dropped, so an event
        // whose constrained properties are all absent yields nothing to constrain. Saving it anyway would
        // reach GetValue with an empty sequence, which hashes the empty string rather than answering null,
        // so every such event would claim one shared SHA-256("") key and the second one would collide while
        // the validator had already waved it through on the very same emptiness.
        //
        // Per-event-source mode lets go of the source's previous claim when the value is cleared. Per-value
        // mode keeps earlier claims until a removal event releases them; an absent value claims nothing.
        var propertiesWithValues = definition.GetPropertiesAndValues(context).ToList();
        if (propertiesWithValues.Count == 0)
        {
            return definition.Mode == UniqueConstraintMode.PerEventSource
                ? new(UniqueConstraintIndexChangeKind.Remove, context.EventSourceId, eventSequenceNumber, scopeKey)
                : null;
        }

        var value = propertiesWithValues.GetValue(definition.IgnoreCasing);
        return new(UniqueConstraintIndexChangeKind.Save, context.EventSourceId, eventSequenceNumber, scopeKey, value, definition.SubjectFor(context));
    }
}
