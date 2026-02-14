// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents the compensation of an event.
/// </summary>
/// <param name="EventTypeGeneration">The <see cref="EventTypeGeneration"/> of the compensation.</param>
/// <param name="CorrelationId">The unique identifier used to correlation.</param>
/// <param name="Causation">The chain of causation.</param>
/// <param name="CausedBy">Who or what caused the event.</param>
/// <param name="Occurred">The time the compensation occurred.</param>
/// <param name="Content">The content per event type generation.</param>
public record EventCompensation(
    EventTypeGeneration EventTypeGeneration,
    CorrelationId CorrelationId,
    IEnumerable<Causation> Causation,
    IdentityId CausedBy,
    DateTimeOffset Occurred,
    IDictionary<string, BsonDocument> Content);
