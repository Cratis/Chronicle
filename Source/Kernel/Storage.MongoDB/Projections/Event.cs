// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Projections;

/// <summary>
/// Represents a simplified MongoDB representation of an event.
/// </summary>
/// <param name="EventSequenceId">The event sequence identifier.</param>
/// <param name="EventType">The event type.</param>
/// <param name="EventSourceId">The event source identifier.</param>
/// <param name="Content">The event content as a BSON document.</param>
public record Event(
    EventSequenceNumber EventSequenceId,
    EventType EventType,
    EventSourceId EventSourceId,
    BsonDocument Content);
