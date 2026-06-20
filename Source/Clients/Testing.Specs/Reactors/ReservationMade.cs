// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// A test event representing a reservation made by a member, used by the ReactorScenario specs.
/// </summary>
/// <param name="MemberId">The identifier of the member that made the reservation.</param>
[EventType]
public record ReservationMade(string MemberId);
