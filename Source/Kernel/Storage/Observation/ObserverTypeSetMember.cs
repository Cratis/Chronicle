// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Represents one event-type identifier's membership, without generation capabilities.
/// </summary>
/// <param name="Incarnation">The revision at which the identifier joined.</param>
/// <param name="JoinFence">The first sequence number eligible for non-replay delivery, or null for an unfenced member.</param>
public record ObserverTypeSetMember(ObserverTypeSetRevision Incarnation, EventSequenceNumber? JoinFence);
