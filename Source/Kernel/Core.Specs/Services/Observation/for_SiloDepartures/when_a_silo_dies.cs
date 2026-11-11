// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_SiloDepartures;

public class when_a_silo_dies : given.two_silos
{
    Task _departure;
    bool _departed;

    async Task Because()
    {
        _departure = _departures.Next;
        Publish((_first, SiloStatus.Active), (_second, SiloStatus.Dead));
        _departed = await CompletesWithin(_departure, Patience);
    }

    [Fact] void should_tell_that_a_silo_left() => _departed.ShouldBeTrue();
    [Fact] void should_wait_for_the_next_departure_afterwards() => _departures.Next.IsCompleted.ShouldBeFalse();
}
