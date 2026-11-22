// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Projections.Definitions;

/// <summary>
/// Represents the definition from for a set of events.
/// </summary>
/// <param name="EventTypes">Collection of <see cref="EventType"/> for the definition.</param>
/// <param name="From">The from definition associated.</param>
/// <remarks>
/// This is typically representing event types that are deriving from a common base type.
/// </remarks>
[Immutable]
[GenerateSerializer]
[Alias("Cratis.Chronicle.Concepts.Projections.Definitions.FromDerivatives")]
public record FromDerivatives(IEnumerable<EventType> EventTypes, [Id(1)] FromDefinition From)
{
    /// <summary>
    /// Gets collection of <see cref="EventType"/> for the definition.
    /// </summary>
    [Id(0)]
    public IEnumerable<EventType> EventTypes { get; init => field = ImmutableDefinitions.Snapshot(value); } = ImmutableDefinitions.Snapshot(EventTypes);
}
