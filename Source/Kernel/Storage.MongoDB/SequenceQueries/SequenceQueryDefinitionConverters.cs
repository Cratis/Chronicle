// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;

namespace Cratis.Chronicle.Storage.MongoDB.SequenceQueries;

/// <summary>
/// Converters between the kernel and MongoDB representations of a saved event sequence query.
/// </summary>
public static class SequenceQueryDefinitionConverters
{
    /// <summary>
    /// Convert to the kernel representation.
    /// </summary>
    /// <param name="definition">The MongoDB <see cref="SequenceQueryDefinition"/> to convert.</param>
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
            new SequenceQueryFilter(
                definition.EventSourceId,
                definition.EventSourceType,
                definition.EventStreamType,
                definition.CorrelationId,
                definition.EventTypes,
                definition.Tags,
                definition.OccurredFrom,
                definition.OccurredTo),
            definition.SortBy,
            definition.Descending);

    /// <summary>
    /// Convert to the MongoDB representation.
    /// </summary>
    /// <param name="definition">The kernel <see cref="Concepts.SequenceQueries.SequenceQueryDefinition"/> to convert.</param>
    /// <returns>The converted <see cref="SequenceQueryDefinition"/>.</returns>
    public static SequenceQueryDefinition ToMongoDB(this Concepts.SequenceQueries.SequenceQueryDefinition definition) =>
        new(
            definition.Id,
            definition.Name,
            definition.Scope,
            definition.Owner,
            definition.Folder,
            definition.Namespace,
            definition.EventSequenceId,
            definition.Filter.EventSourceId,
            definition.Filter.EventSourceType,
            definition.Filter.EventStreamType,
            definition.Filter.CorrelationId,
            [.. definition.Filter.EventTypes],
            [.. definition.Filter.Tags],
            definition.Filter.OccurredFrom,
            definition.Filter.OccurredTo,
            definition.SortBy,
            definition.Descending);

    /// <summary>
    /// Convert a folder to the kernel representation.
    /// </summary>
    /// <param name="folder">The MongoDB <see cref="SequenceQueryFolder"/> to convert.</param>
    /// <returns>The converted <see cref="SequenceQueryFolderDefinition"/>.</returns>
    public static SequenceQueryFolderDefinition ToKernel(this SequenceQueryFolder folder) =>
        new(folder.Id, folder.Scope, folder.Owner, folder.Namespace, folder.Path);

    /// <summary>
    /// Convert a folder to the MongoDB representation.
    /// </summary>
    /// <param name="folder">The kernel <see cref="SequenceQueryFolderDefinition"/> to convert.</param>
    /// <returns>The converted <see cref="SequenceQueryFolder"/>.</returns>
    public static SequenceQueryFolder ToMongoDB(this SequenceQueryFolderDefinition folder) =>
        new(folder.Id, folder.Scope, folder.Owner, folder.Namespace, folder.Path);
}
