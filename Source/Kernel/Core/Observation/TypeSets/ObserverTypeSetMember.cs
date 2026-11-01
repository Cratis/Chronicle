// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.TypeSets;

/// <summary>
/// Represents an event type's membership independently of generation.
/// </summary>
/// <param name="EventTypeId">The event type identifier.</param>
/// <param name="Incarnation">The revision at which the identifier joined.</param>
/// <param name="JoinFence">The first sequence number eligible for non-replay delivery, or null if unfenced.</param>
public record ObserverTypeSetMember(string EventTypeId, long Incarnation, ObserverTypeSetFence? JoinFence);
