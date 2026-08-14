// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Concepts.SequenceQueries;

/// <summary>
/// Represents an event sequence query a user saved so it can be reopened later.
/// </summary>
/// <param name="Id">The unique identifier of the query.</param>
/// <param name="Name">The display name the user gave it.</param>
/// <param name="Scope">Who the query is visible to.</param>
/// <param name="Owner">The identity that saved it.</param>
/// <param name="Folder">The folder within the scope the query is filed under.</param>
/// <param name="Namespace">The namespace the query runs against.</param>
/// <param name="EventSequenceId">The event sequence the query runs against.</param>
/// <param name="Filter">The narrowing the user configured.</param>
/// <param name="SortBy">What the results are ordered by.</param>
/// <param name="Descending">Whether results are ordered from the highest value down rather than from the lowest up.</param>
public record SequenceQueryDefinition(
    SequenceQueryId Id,
    SequenceQueryName Name,
    SequenceQueryScope Scope,
    SequenceQueryOwner Owner,
    SequenceQueryFolder Folder,
    EventStoreNamespaceName Namespace,
    EventSequenceId EventSequenceId,
    SequenceQueryFilter Filter,
    SequenceQuerySortBy SortBy,
    bool Descending);
