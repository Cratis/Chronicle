// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.SequenceQueries.Listing;

/// <summary>
/// Represents a saved event sequence query nested inside its containing
/// <see cref="EventSequenceQueryFolder"/> read model.
/// </summary>
/// <param name="QueryId">The unique identifier of the query.</param>
/// <param name="Name">The display name of the query.</param>
/// <param name="EventSequenceId">The event sequence the query targets.</param>
/// <param name="Filter">The filter definition for the query.</param>
public record EventSequenceQuery(
    SequenceQueryId QueryId,
    string Name,
    string EventSequenceId,
    SequenceQueryFilter Filter);
