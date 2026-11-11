// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_SiloDepartures;

/// <summary>
/// A silo that is shutting down still hosts activations it has not deactivated yet, so it has not left.
/// </summary>
public class when_a_silo_is_shutting_down : given.two_silos
{
    Task _departure;
    bool _departed;

    async Task Because()
    {
        _departure = _departures.Next;
        Publish((_first, SiloStatus.Active), (_second, SiloStatus.ShuttingDown));
        _departed = await CompletesWithin(_departure, TimeSpan.FromMilliseconds(200));
    }

    [Fact] void should_not_tell_that_a_silo_left() => _departed.ShouldBeFalse();
}
