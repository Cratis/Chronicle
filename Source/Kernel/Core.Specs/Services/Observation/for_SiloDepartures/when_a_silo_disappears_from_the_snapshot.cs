// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_SiloDepartures;

/// <summary>
/// A silo the membership no longer lists has left, even if it was never seen as dead.
/// </summary>
public class when_a_silo_disappears_from_the_snapshot : given.two_silos
{
    Task _departure;

    void Because()
    {
        _departure = _departures.Next;
        Publish((_first, SiloStatus.Active));
    }

    [Fact] void should_tell_that_a_silo_left() => _departure.IsCompleted.ShouldBeTrue();
}
