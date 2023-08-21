// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Auditing;
using Aksio.Cratis.Events;
using Aksio.Cratis.Identities;
using MongoDB.Bson;

#nullable disable

namespace Aksio.Cratis.Kernel.MongoDB;

/// <summary>
/// Represents the compensation of an event.
/// </summary>
/// <param name="CorrelationId">The unique identifier used to correlation.</param>
/// <param name="Causation">The chain of causation.</param>
/// <param name="CausedBy">Who or what caused the event.</param>
/// <param name="Occurred">The time the compensation occurred.</param>
/// <param name="ValidFrom">The date and time the compensation is valid from.</param>
/// <param name="Content">The content per event type generation.</param>
public record EventCompensation(
    CorrelationId CorrelationId,
    IEnumerable<Causation> Causation,
    IdentityId CausedBy,
    DateTimeOffset Occurred,
    DateTimeOffset ValidFrom,
    Dictionary<EventGeneration, BsonDocument> Content);
