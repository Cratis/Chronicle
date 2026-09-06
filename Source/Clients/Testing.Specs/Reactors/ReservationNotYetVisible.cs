// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// The exception that is thrown by <see cref="ThrowingReactor"/> to simulate a reactor handler rejecting an
/// event because the read model it depends on has not caught up yet.
/// </summary>
public class ReservationNotYetVisible() : Exception("Reservation is not yet visible");
