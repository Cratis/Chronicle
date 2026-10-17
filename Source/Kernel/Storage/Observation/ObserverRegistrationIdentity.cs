// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Represents persisted registration capabilities, not a connected delivery target.
/// </summary>
/// <param name="ConnectionId">The declaring connection.</param>
/// <param name="RegistrationId">The optional caller-supplied identity used to correlate registration status.</param>
/// <param name="DeclaredTypes">The declared event types with their generation pins.</param>
/// <param name="GenerationDelivery">The connection's event generation delivery mode.</param>
public record ObserverRegistrationIdentity(
    ConnectionId ConnectionId,
    Guid? RegistrationId,
    IReadOnlyList<EventType> DeclaredTypes,
    EventGenerationDelivery GenerationDelivery);
