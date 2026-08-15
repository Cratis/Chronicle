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
    public async Task Update(EventSequenceNumber eventSequenceNumber)
    {
        var scopeKey = definition.Scope.BuildScopeKey(context.EventSourceType, context.EventStreamType, context.EventStreamId);

        // Any of the declared removal events releases the claim on its own — a lifecycle that ends in more than
        // one way has more than one terminal fact, and each of them frees the value.
        if (definition.RemovedWith.Contains(context.EventTypeId))
        {
            await storage.Remove(context.EventSourceId, definition.Name, scopeKey);
        }
        else
        {
            if (!definition.SupportsEventType(context.EventTypeId))
            {
                return;
            }

            var value = definition.GetPropertiesAndValues(context).GetValue(definition.IgnoreCasing);
            if (value is not null)
            {
                await storage.Save(context.EventSourceId, definition.Name, eventSequenceNumber, value, scopeKey);
            }
        }
    }
}
