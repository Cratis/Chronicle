// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.TypeSets;

/// <summary>
/// Represents a stored declaration, without asserting that the connection is still connected.
/// </summary>
/// <param name="ConnectionId">The declaring connection.</param>
/// <param name="RegistrationId">The optional identity used to correlate warnings.</param>
/// <param name="DeclaredTypes">The declared event types with their generation pins.</param>
public record ObserverTypeSetRegistration(string ConnectionId, Guid? RegistrationId, IEnumerable<ObserverDeclaredEventType> DeclaredTypes);
