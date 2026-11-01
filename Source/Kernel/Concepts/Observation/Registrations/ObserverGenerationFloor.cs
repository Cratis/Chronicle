// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Registrations;

/// <summary>
/// Represents the event-store deployment generation floor, independent of observer revisions.
/// </summary>
/// <param name="Generation">The minimum accepted deployment generation.</param>
/// <param name="Version">The compare-and-set version of the floor record.</param>
public record ObserverGenerationFloor(ulong Generation, ulong Version);
