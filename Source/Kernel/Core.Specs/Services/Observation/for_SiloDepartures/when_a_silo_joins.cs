// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Services.Observation.for_SiloDepartures;

public class when_a_silo_joins : given.two_silos
{
    Task _departure;

    void Because()
    {
        _departure = _departures.Next;
        var third = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11113), 1);
        Publish((_first, SiloStatus.Active), (_second, SiloStatus.Active), (third, SiloStatus.Joining));
        Publish((_first, SiloStatus.Active), (_second, SiloStatus.Active), (third, SiloStatus.Active));
    }

    [Fact] void should_not_tell_that_a_silo_left() => _departure.IsCompleted.ShouldBeFalse();
}
