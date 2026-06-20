// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// A test reactor that records member activity against the member's own event source id —
/// a different event source than the one that triggered the reactor.
/// </summary>
public class ReservationReactor : IReactor
{
    /// <summary>
    /// Reacts to a <see cref="ReservationMade"/> by appending a <see cref="MemberActivityRecorded"/>
    /// to the member's event source id.
    /// </summary>
    /// <param name="event">The triggering <see cref="ReservationMade"/> event.</param>
    /// <returns>An <see cref="EventForEventSourceId"/> targeting the member's event source id.</returns>
    public EventForEventSourceId ReservationMade(ReservationMade @event) =>
        new(@event.MemberId, new MemberActivityRecorded());
}
