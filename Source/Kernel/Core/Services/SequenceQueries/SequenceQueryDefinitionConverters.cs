// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;

namespace Cratis.Chronicle.Services.SequenceQueries;

/// <summary>
/// Converters between the kernel and contract representations of a saved event sequence query.
/// </summary>
public static class SequenceQueryDefinitionConverters
{
    /// <summary>
    /// Convert to the contract representation.
    /// </summary>
    /// <param name="definition">The kernel <see cref="SequenceQueryDefinition"/> to convert.</param>
    /// <returns>The converted <see cref="Contracts.SequenceQueries.SequenceQueryDefinition"/>.</returns>
    public static Contracts.SequenceQueries.SequenceQueryDefinition ToContract(this SequenceQueryDefinition definition) =>
        new()
        {
            Id = definition.Id,
            Name = definition.Name,
            Scope = (Contracts.SequenceQueries.SequenceQueryScope)definition.Scope,
            Owner = definition.Owner,
            Folder = definition.Folder,
            Namespace = definition.Namespace,
            EventSequenceId = definition.EventSequenceId,
            EventSourceId = definition.Filter.EventSourceId,
            EventSourceType = definition.Filter.EventSourceType,
            EventStreamType = definition.Filter.EventStreamType,
            CorrelationId = definition.Filter.CorrelationId,
            EventTypes = [.. definition.Filter.EventTypes],
            Tags = [.. definition.Filter.Tags],
            OccurredFrom = definition.Filter.OccurredFrom,
            OccurredTo = definition.Filter.OccurredTo,
            SortBy = (Contracts.SequenceQueries.SequenceQuerySortBy)definition.SortBy,
            Descending = definition.Descending
        };

    /// <summary>
    /// Convert to the kernel representation.
    /// </summary>
    /// <param name="definition">The contract <see cref="Contracts.SequenceQueries.SequenceQueryDefinition"/> to convert.</param>
    /// <returns>The converted <see cref="SequenceQueryDefinition"/>.</returns>
    public static SequenceQueryDefinition ToKernel(this Contracts.SequenceQueries.SequenceQueryDefinition definition) =>
        new(
            definition.Id,
            definition.Name,
            (SequenceQueryScope)definition.Scope,
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
            (SequenceQuerySortBy)definition.SortBy,
            definition.Descending);

    /// <summary>
    /// Convert a folder to the contract representation.
    /// </summary>
    /// <param name="folder">The kernel <see cref="SequenceQueryFolderDefinition"/> to convert.</param>
    /// <returns>The converted <see cref="Contracts.SequenceQueries.SequenceQueryFolderDefinition"/>.</returns>
    public static Contracts.SequenceQueries.SequenceQueryFolderDefinition ToContract(this SequenceQueryFolderDefinition folder) =>
        new()
        {
            Id = folder.Id,
            Scope = (Contracts.SequenceQueries.SequenceQueryScope)folder.Scope,
            Owner = folder.Owner,
            Namespace = folder.Namespace,
            Path = folder.Path
        };

    /// <summary>
    /// Convert a folder to the kernel representation.
    /// </summary>
    /// <param name="folder">The contract <see cref="Contracts.SequenceQueries.SequenceQueryFolderDefinition"/> to convert.</param>
    /// <returns>The converted <see cref="SequenceQueryFolderDefinition"/>.</returns>
    public static SequenceQueryFolderDefinition ToKernel(this Contracts.SequenceQueries.SequenceQueryFolderDefinition folder) =>
        new(
            folder.Id,
            (SequenceQueryScope)folder.Scope,
            folder.Owner,
            folder.Namespace,
            folder.Path);
}
