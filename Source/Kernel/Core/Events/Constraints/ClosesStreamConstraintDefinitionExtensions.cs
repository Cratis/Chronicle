// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Resolves event-driven closing scopes without widening missing dimensions.
/// </summary>
internal static class ClosesStreamConstraintDefinitionExtensions
{
    /// <summary>
    /// Resolve the exact dimensions a closing or reopening event addresses.
    /// </summary>
    /// <param name="definition">The declaration.</param>
    /// <param name="context">The event context and payload.</param>
    /// <param name="scope">The resolved scope.</param>
    /// <returns>False if a participating value is missing, the mask is invalid, or the scope is default-stream-only.</returns>
    internal static bool TryResolveScope(this ClosesStreamConstraintDefinition definition, ConstraintValidationContext context, out ClosedStreamScope scope)
    {
        const ClosedStreamDimensions all = ClosedStreamDimensions.EventSourceId | ClosedStreamDimensions.EventSourceType | ClosedStreamDimensions.EventStreamType | ClosedStreamDimensions.EventStreamId;
        scope = new();
        if (definition.Name.Value.Length == 0 || definition.Dimensions == ClosedStreamDimensions.None || (definition.Dimensions & ~all) != ClosedStreamDimensions.None) return false;

        var streamId = context.EventStreamId ?? EventStreamId.Default;
        if (definition.EventStreamIdFrom is not null)
        {
            if (!definition.Dimensions.HasFlag(ClosedStreamDimensions.EventStreamId)) return false;
            var value = new PropertyPath(definition.EventStreamIdFrom).GetValue(context.Content, ArrayIndexers.NoIndexers)?.ToString();
            if (string.IsNullOrEmpty(value)) return false;
            streamId = value;
        }

        scope = ClosedStreamScope.ForAppend(context.EventSourceId, context.EventSourceType ?? EventSourceType.Default, context.EventStreamType ?? EventStreamType.All, streamId, definition.Dimensions);

        return !scope.IsEmpty && scope.Dimensions == definition.Dimensions && !scope.IsDefaultStreamOnly;
    }
}
