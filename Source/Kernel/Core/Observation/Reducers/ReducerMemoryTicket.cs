// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Memory;

namespace Cratis.Chronicle.Observation.Reducers;

/// <summary>
/// Represents the memory a reduce holds from the <see cref="IMemoryBudget"/>, which can change once the size of the state is known.
/// </summary>
/// <param name="reservation">The <see cref="MemoryReservation"/> held when the reduce starts.</param>
public sealed class ReducerMemoryTicket(MemoryReservation reservation) : IDisposable
{
    /// <summary>
    /// Gets a ticket that holds nothing.
    /// </summary>
    public static ReducerMemoryTicket None => new(MemoryReservation.None);

    /// <summary>
    /// Gets the <see cref="MemoryReservation"/> currently held.
    /// </summary>
    public MemoryReservation Reservation { get; internal set; } = reservation;

    /// <inheritdoc/>
    public void Dispose() => Reservation.Dispose();
}
