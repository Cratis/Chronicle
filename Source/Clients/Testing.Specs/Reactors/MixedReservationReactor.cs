// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// A test reactor that returns a mix of a bare event (to the triggering source) and an
/// <see cref="EventForEventSourceId"/> (to the member) in a single return.
/// </summary>
public class MixedReservationReactor : IReactor
{
    /// <summary>
    /// Reacts to a <see cref="ReservationMade"/> by logging activity on the triggering source and on the member.
    /// </summary>
    /// <param name="event">The triggering <see cref="ReservationMade"/> event.</param>
    /// <returns>A mixed collection of side effects.</returns>
    public IEnumerable<object> ReservationMade(ReservationMade @event) =>
    [
        new MemberActivityRecorded(),
        new EventForEventSourceId(@event.MemberId, new MemberActivityRecorded())
    ];
}
