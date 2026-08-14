// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.SequenceQueries;

namespace Cratis.Chronicle.Api.SequenceQueries;

/// <summary>
/// Converters between the contract and API representations of saved event sequence queries.
/// </summary>
/// <remarks>
/// These live beside the read models rather than on them, because every static method on a
/// <c>[ReadModel]</c> is a query - a conversion helper declared there would be published as an
/// endpoint of its own.
/// </remarks>
public static class SequenceQueryConverters
{
    /// <summary>
    /// Convert a saved query to its API representation.
    /// </summary>
    /// <param name="definition">The <see cref="SequenceQueryDefinition"/> to convert.</param>
    /// <returns>The converted <see cref="SequenceQuery"/>.</returns>
    public static SequenceQuery ToApi(this SequenceQueryDefinition definition) =>
        new(
            definition.Id,
            definition.Name,
            (SequenceQueryScope)definition.Scope,
            definition.Owner,
            definition.Folder,
            definition.Namespace,
            definition.EventSequenceId,
            definition.EventSourceId,
            definition.EventSourceType,
            definition.EventStreamType,
            definition.CorrelationId,
            definition.EventTypes,
            definition.Tags,
            definition.OccurredFrom,
            definition.OccurredTo,
            definition.SortBy.ToString(),
            definition.Descending);

    /// <summary>
    /// Convert a folder to its API representation.
    /// </summary>
    /// <param name="definition">The <see cref="SequenceQueryFolderDefinition"/> to convert.</param>
    /// <returns>The converted <see cref="QueryFolder"/>.</returns>
    public static QueryFolder ToApi(this SequenceQueryFolderDefinition definition) =>
        new(
            definition.Id,
            (SequenceQueryScope)definition.Scope,
            definition.Owner,
            definition.Namespace,
            definition.Path);
}
