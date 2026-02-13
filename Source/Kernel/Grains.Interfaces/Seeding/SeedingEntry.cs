// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.Seeding;

/// <summary>
/// Represents a seeding entry to be processed.
/// </summary>
/// <param name="EventSourceId">The event source identifier.</param>
/// <param name="EventTypeId">The event type identifier.</param>
/// <param name="Content">The JSON content.</param>
/// <param name="Tags">The collection of tags associated with the event.</param>
public record SeedingEntry(EventSourceId EventSourceId, EventTypeId EventTypeId, string Content, IEnumerable<Tag>? Tags = null);
