// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.SequenceQueries;

/// <summary>
/// Converters between the kernel and SQL representations of a saved event sequence query.
/// </summary>
public static class SequenceQueryDefinitionConverters
{
    /// <summary>
    /// Convert to the kernel representation.
    /// </summary>
    /// <param name="definition">The SQL <see cref="SequenceQueryDefinition"/> to convert.</param>
    /// <returns>The converted <see cref="Concepts.SequenceQueries.SequenceQueryDefinition"/>.</returns>
    public static Concepts.SequenceQueries.SequenceQueryDefinition ToKernel(this SequenceQueryDefinition definition) =>
        new(
            definition.Id,
            definition.Name,
            definition.Scope,
            definition.Owner,
            definition.Folder,
            definition.Namespace,
            definition.EventSequenceId,
            new Concepts.SequenceQueries.SequenceQueryFilter(
                definition.Filter.EventSourceId,
                definition.Filter.EventSourceType,
                definition.Filter.EventStreamType,
                definition.Filter.CorrelationId,
                definition.Filter.EventTypes,
                definition.Filter.Tags,
                definition.Filter.OccurredFrom,
                definition.Filter.OccurredTo),
            definition.SortBy,
            definition.Descending);

    /// <summary>
    /// Convert to the SQL representation.
    /// </summary>
    /// <param name="definition">The kernel <see cref="Concepts.SequenceQueries.SequenceQueryDefinition"/> to convert.</param>
    /// <returns>The converted <see cref="SequenceQueryDefinition"/>.</returns>
    public static SequenceQueryDefinition ToSql(this Concepts.SequenceQueries.SequenceQueryDefinition definition) =>
        new()
        {
            Id = definition.Id,
            Name = definition.Name,
            Scope = definition.Scope,
            Owner = definition.Owner,
            Folder = definition.Folder,
            Namespace = definition.Namespace,
            EventSequenceId = definition.EventSequenceId,
            Filter = new SequenceQueryFilter
            {
                EventSourceId = definition.Filter.EventSourceId,
                EventSourceType = definition.Filter.EventSourceType,
                EventStreamType = definition.Filter.EventStreamType,
                CorrelationId = definition.Filter.CorrelationId,
                EventTypes = [.. definition.Filter.EventTypes],
                Tags = [.. definition.Filter.Tags],
                OccurredFrom = definition.Filter.OccurredFrom,
                OccurredTo = definition.Filter.OccurredTo
            },
            SortBy = definition.SortBy,
            Descending = definition.Descending
        };

    /// <summary>
    /// Convert a folder to the kernel representation.
    /// </summary>
    /// <param name="folder">The SQL <see cref="SequenceQueryFolder"/> to convert.</param>
    /// <returns>The converted <see cref="Concepts.SequenceQueries.SequenceQueryFolderDefinition"/>.</returns>
    public static Concepts.SequenceQueries.SequenceQueryFolderDefinition ToKernel(this SequenceQueryFolder folder) =>
        new(folder.Id, folder.Scope, folder.Owner, folder.Namespace, folder.Path);

    /// <summary>
    /// Convert a folder to the SQL representation.
    /// </summary>
    /// <param name="folder">The kernel <see cref="Concepts.SequenceQueries.SequenceQueryFolderDefinition"/> to convert.</param>
    /// <returns>The converted <see cref="SequenceQueryFolder"/>.</returns>
    public static SequenceQueryFolder ToSql(this Concepts.SequenceQueries.SequenceQueryFolderDefinition folder) =>
        new()
        {
            Id = folder.Id,
            Scope = folder.Scope,
            Owner = folder.Owner,
            Namespace = folder.Namespace,
            Path = folder.Path
        };
}
