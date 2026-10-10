// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Maintains one constraint's closures after a durable event append.
/// </summary>
/// <param name="definition">The owning closing declaration.</param>
/// <param name="context">The event and its resolved dimensions.</param>
/// <param name="storage">The closure storage.</param>
/// <remarks>
/// The event sequence executes this update in the same grain turn as the append, so another append cannot
/// interleave. The event and closure are not crash-atomic; an explicit reindex repairs a lost index update.
/// Reopening removes only this owner's exact row, never a manual closure or another constraint's closure.
/// </remarks>
public class ClosesStreamIndexUpdater(ClosesStreamConstraintDefinition definition, ConstraintValidationContext context, IClosedStreamsConstraintStorage storage) : IUpdateConstraintIndex
{
    /// <inheritdoc/>
    /// <exception cref="InvalidClosingStreamScope">A relevant event cannot resolve its exact scope.</exception>
    public async Task Update(EventSequenceNumber eventSequenceNumber)
    {
        var reopening = definition.ReopenedBy.Contains(context.EventTypeId);
        if (!reopening && !definition.EventTypeIds.Contains(context.EventTypeId)) return;
        if (!definition.TryResolveScope(context, out var scope)) throw new InvalidClosingStreamScope(definition.Name);

        if (reopening)
        {
            await storage.Reopen(definition.Name.Value, scope);
        }
        else
        {
            await storage.Close(new(scope, definition.Name.Value, eventSequenceNumber, DateTimeOffset.UtcNow));
        }
    }
}
