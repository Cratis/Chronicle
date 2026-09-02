// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Converts stored event type definitions into the read model the event type queries answer with.
/// </summary>
/// <remarks>
/// These live beside the read model rather than on it because a static method on a <c>[ReadModel]</c> whose
/// return shape is a supported query shape becomes a generated query proxy and an HTTP endpoint - accessibility
/// is not what the proxy generator looks at.
/// </remarks>
internal static class EventTypeDetailsConverters
{
    /// <summary>
    /// Converts stored definitions into read models.
    /// </summary>
    /// <param name="definitions">The stored definitions.</param>
    /// <returns>The definitions as read models.</returns>
    internal static IEnumerable<EventTypeDetails> ToReadModel(this IEnumerable<Concepts.EventTypes.EventTypeSchema> definitions) =>
        [.. definitions.Select(ToReadModel)];

    /// <summary>
    /// Converts a stored definition into a read model.
    /// </summary>
    /// <param name="definition">The stored definition.</param>
    /// <returns>The definition as a read model.</returns>
    internal static EventTypeDetails ToReadModel(this Concepts.EventTypes.EventTypeSchema definition) =>
        new(
            definition.Type.Id,
            Concepts.Events.EventTypeConverters.ToContract(definition.Type),
            (Contracts.Events.EventTypeOwner)(int)definition.Owner,
            (Contracts.Events.EventTypeSource)(int)definition.Source,
            definition.Schema.ToJson());
}
