// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.SequenceQueries;

/// <summary>
/// Converts stored event sequence queries into the read models the workbench queries.
/// </summary>
/// <remarks>
/// These live beside the read models rather than on them, because every static method on a
/// <c>[ReadModel]</c> whose return shape is a supported query shape is published as a query proxy and an
/// endpoint - accessibility is not what the proxy generator looks at.
/// </remarks>
public static class SequenceQueryConverters
{
    /// <summary>
    /// Converts stored queries into read models.
    /// </summary>
    /// <param name="definitions">The stored queries.</param>
    /// <returns>The queries as read models.</returns>
    internal static IEnumerable<SequenceQuery> ToReadModel(this IEnumerable<Concepts.SequenceQueries.SequenceQueryDefinition> definitions) =>
        [.. definitions.Select(ToReadModel)];

    /// <summary>
    /// Converts a stored query into a read model.
    /// </summary>
    /// <param name="definition">The stored query.</param>
    /// <returns>The query as a read model.</returns>
    internal static SequenceQuery ToReadModel(this Concepts.SequenceQueries.SequenceQueryDefinition definition) =>
        new(
            definition.Id,
            definition.Name,
            definition.Scope,
            definition.Owner,
            definition.Folder,
            definition.Namespace,
            definition.EventSequenceId,
            definition.Filter.EventSourceId ?? string.Empty,
            definition.Filter.EventSourceType ?? string.Empty,
            definition.Filter.EventStreamType ?? string.Empty,
            definition.Filter.CorrelationId ?? string.Empty,
            definition.Filter.EventTypes ?? [],
            definition.Filter.Tags ?? [],
            definition.Filter.OccurredFrom,
            definition.Filter.OccurredTo,
            definition.SortBy.ToString(),
            definition.Descending);

    /// <summary>
    /// Converts stored folders into read models.
    /// </summary>
    /// <param name="definitions">The stored folders.</param>
    /// <returns>The folders as read models.</returns>
    internal static IEnumerable<QueryFolder> ToReadModel(this IEnumerable<Concepts.SequenceQueries.SequenceQueryFolderDefinition> definitions) =>
        [.. definitions.Select(ToReadModel)];

    /// <summary>
    /// Converts a stored folder into a read model.
    /// </summary>
    /// <param name="definition">The stored folder.</param>
    /// <returns>The folder as a read model.</returns>
    internal static QueryFolder ToReadModel(this Concepts.SequenceQueries.SequenceQueryFolderDefinition definition) =>
        new(
            definition.Id,
            definition.Scope,
            definition.Owner,
            definition.Namespace,
            definition.Path);
}
