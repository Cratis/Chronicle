// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.TypeSets;

/// <summary>
/// Represents an event type declared by a registration.
/// </summary>
/// <param name="Id">The event type identifier.</param>
/// <param name="Generation">The pinned generation.</param>
/// <param name="Tombstone">Whether the declared event is a tombstone.</param>
public record ObserverDeclaredEventType(string Id, uint Generation, bool Tombstone);
