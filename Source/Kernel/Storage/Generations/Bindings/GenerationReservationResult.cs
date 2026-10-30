// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Generations.Bindings;

/// <summary>
/// Represents the generation reservation result.
/// </summary>
public enum GenerationReservationResult
{
    /// <summary>
    /// The candidate reservation was created.
    /// </summary>
    ReservationCreated = 0,

    /// <summary>
    /// UnknownMaterialization.
    /// </summary>
    UnknownMaterialization = 1,

    /// <summary>
    /// NotBound.
    /// </summary>
    NotBound = 2,

    /// <summary>
    /// CandidateInFlight.
    /// </summary>
    CandidateInFlight = 3,

    /// <summary>
    /// DestinationOccupied.
    /// </summary>
    DestinationOccupied = 4
}
