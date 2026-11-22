// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Seeding;

/// <summary>
/// Represents a seeding entry to be processed.
/// </summary>
/// <param name="EventSourceId">The event source identifier.</param>
/// <param name="EventTypeId">The event type identifier.</param>
/// <param name="Content">The JSON content.</param>
/// <param name="Tags">The collection of tags associated with the event.</param>
/// <param name="EventSourceType">The optional event source type.</param>
/// <param name="EventStreamType">The optional event stream type.</param>
/// <param name="EventStreamId">The optional event stream identifier.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Seeding.SeedingEntry")]
public record SeedingEntry(
    [Id(0)] EventSourceId EventSourceId,
    [Id(1)] EventTypeId EventTypeId,
    [Id(2)] string Content,
    [Id(3)] IEnumerable<Tag>? Tags,
    [Id(4)] EventSourceType? EventSourceType = default,
    [Id(5)] EventStreamType? EventStreamType = default,
    [Id(6)] EventStreamId? EventStreamId = default);
