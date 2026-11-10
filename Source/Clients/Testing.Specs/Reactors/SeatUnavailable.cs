// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// The exception that a batch reactor fails a partition with when a seat cannot be indexed.
/// </summary>
/// <param name="seat">The seat.</param>
public class SeatUnavailable(string seat) : Exception($"Seat '{seat}' is unavailable");
