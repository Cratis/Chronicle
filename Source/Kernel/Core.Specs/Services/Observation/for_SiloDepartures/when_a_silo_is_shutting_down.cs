// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_SiloDepartures;

/// <summary>
/// A silo that shuts down gracefully deactivates its activations while it does, so it counts as leaving from the start.
/// </summary>
public class when_a_silo_is_shutting_down : given.two_silos
{
    Task _departure;

    void Because()
    {
        _departure = _departures.Next;
        Publish((_first, SiloStatus.Active), (_second, SiloStatus.ShuttingDown));
    }

    [Fact] void should_tell_that_a_silo_is_leaving() => _departure.IsCompleted.ShouldBeTrue();
}
