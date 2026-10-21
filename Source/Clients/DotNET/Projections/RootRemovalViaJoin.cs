// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Remembers which event types a fluent projection declared as remove via join at its root.
/// </summary>
/// <remarks>
/// The fluent root builder does not put those declarations into the <see cref="ProjectionDefinition"/> sent to the
/// kernel, and that must stay so: changing the definition would change what is stored and could trigger replays. The
/// declaration is kept on the side, keyed by the definition it belongs to, so registration can warn about it.
/// </remarks>
internal static class RootRemovalViaJoin
{
    static readonly ConditionalWeakTable<ProjectionDefinition, IReadOnlyList<string>> _declared = [];

    /// <summary>
    /// Record the event types declared as remove via join at the root of a definition.
    /// </summary>
    /// <param name="definition">The <see cref="ProjectionDefinition"/> built.</param>
    /// <param name="eventTypes">The identifiers of the declared event types.</param>
    internal static void Record(ProjectionDefinition definition, IReadOnlyList<string> eventTypes)
    {
        if (eventTypes.Count > 0)
        {
            _declared.AddOrUpdate(definition, eventTypes);
        }
    }

    /// <summary>
    /// Get the identifiers of the event types a definition declares as remove via join at its root.
    /// </summary>
    /// <param name="definition">The <see cref="ProjectionDefinition"/> to get for.</param>
    /// <returns>The event type identifiers, empty when there are none.</returns>
    internal static IReadOnlyList<string> EventTypesFor(ProjectionDefinition definition)
    {
        if (definition.RemovedWithJoin.Count > 0)
        {
            return [.. definition.RemovedWithJoin.Keys.Select(_ => _.Id)];
        }

        return _declared.TryGetValue(definition, out var declared) ? declared : [];
    }
}
