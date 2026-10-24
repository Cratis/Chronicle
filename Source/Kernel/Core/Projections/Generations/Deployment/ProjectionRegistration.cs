// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Generations.Deployment;

/// <summary>
/// A connected client's projection registration, observed on its silo in memory.
/// </summary>
/// <param name="ConnectionId">The declaring connection.</param>
/// <param name="EventStore">The projection's event store.</param>
/// <param name="Projection">The declared projection.</param>
/// <param name="RegisteredVersion">The raw definition registration version, absent for legacy declarations.</param>
/// <param name="RegisteredAt">When this connection first registered this version without interruption.</param>
[GenerateSerializer]
public record ProjectionRegistration(
    [property: Id(0)] ConnectionId ConnectionId,
    [property: Id(1)] EventStoreName EventStore,
    [property: Id(2)] ProjectionId Projection,
    [property: Id(3)] string? RegisteredVersion,
    [property: Id(4)] DateTimeOffset RegisteredAt);
