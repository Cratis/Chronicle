// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Api.SequenceQueries.Events;

/// <summary>
/// Event raised when an event sequence query is added to a folder.
/// </summary>
/// <param name="FolderId">The folder this query belongs to.</param>
/// <param name="Name">The display name of the query.</param>
/// <param name="EventSequenceId">The event sequence the query targets.</param>
/// <param name="Filter">The serialized filter definition.</param>
[EventType]
public record EventSequenceQueryAdded(
    EventSequenceQueryFolderId FolderId,
    string Name,
    string EventSequenceId,
    SequenceQueryFilter Filter);
