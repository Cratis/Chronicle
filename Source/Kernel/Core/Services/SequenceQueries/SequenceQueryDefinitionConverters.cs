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
            EventTypes = [.. definition.Filter.EventTypes],
            Tags = [.. definition.Filter.Tags],
            OccurredFrom = definition.Filter.OccurredFrom,
            OccurredTo = definition.Filter.OccurredTo,
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
                definition.EventTypes,
                definition.Tags,
                definition.OccurredFrom,
                definition.OccurredTo),
            definition.Descending);
}
