// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Seeding;

/// <summary>
/// Represents a single seeded event entry.
/// </summary>
/// <param name="EventSourceId">The event source identifier.</param>
/// <param name="EventTypeId">The event type identifier.</param>
/// <param name="Content">The JSON content of the event.</param>
/// <param name="Tags">The collection of tags associated with the event.</param>
public record SeededEventEntry(EventSourceId EventSourceId, EventTypeId EventTypeId, string Content, IEnumerable<string>? Tags);
