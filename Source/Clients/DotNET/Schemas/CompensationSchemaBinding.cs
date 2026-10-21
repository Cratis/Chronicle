// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Sinks;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Distinguishes new compensation bindings from declaration-only metadata, without requiring legacy targets to be registered.
/// </summary>
internal static class CompensationSchemaBinding
{
    /// <summary>
    /// Determines whether a compensation target actually requires an explicit registry identity.
    /// </summary>
    /// <param name="type">The schema's CLR type.</param>
    /// <param name="eventTypes">The owning store's registry.</param>
    /// <returns>Whether the declaration-only generation path cannot supply the identity.</returns>
    internal static bool RequiresRegistry(Type type, IEventTypes eventTypes) =>
        type.GetCustomAttribute<CompensationForAttribute>() is { } compensation && RequiresTargetRegistry(compensation.CompensatedEventType, eventTypes);

    /// <summary>
    /// Resolves a compensation identity using declaration semantics unless a new binding is needed.
    /// </summary>
    /// <param name="target">The compensated event's CLR type.</param>
    /// <param name="eventTypes">The owning registry.</param>
    /// <returns>The compensation identifier.</returns>
    internal static EventTypeId Resolve(Type target, IEventTypes eventTypes) => RequiresTargetRegistry(target, eventTypes)
        ? eventTypes.GetEventTypeFor(target).Id
        : target.GetEventType().Id;

    static bool RequiresTargetRegistry(Type target, IEventTypes eventTypes)
    {
        if (!target.IsEventTypeTarget())
        {
            // GetEventTypeFor supplies the existing missing-membership diagnostic rather than guessing a CLR name.
            eventTypes.GetEventTypeFor(target);
            return true;
        }

        // An omitted marked target is intentionally declaration-only. Custom generators must receive exactly
        // their old call and result here, even if the registry does not contain the target.
        if (!eventTypes.HasFor(target)) return false;
        if (target.GetCustomAttribute<EventTypeGenerationForAttribute>() is { } historical &&
            !historical.EventTypeClrType.IsDefined(typeof(EventTypeAttribute)) && eventTypes.HasFor(historical.EventTypeClrType))
        {
            return true;
        }

        return target.GetEventType().Id != eventTypes.GetEventTypeFor(target).Id;
    }
}
