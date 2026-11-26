// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_Shutdown;

public class when_using_defaults : Specification
{
    Shutdown _shutdown;

    void Because() => _shutdown = new();

    [Fact] void should_drain_for_eight_seconds() => _shutdown.DrainTimeout.ShouldEqual(TimeSpan.FromSeconds(8));
    [Fact] void should_reserve_five_seconds_for_host_stop() => _shutdown.HostShutdownReserve.ShouldEqual(TimeSpan.FromSeconds(5));
    [Fact] void should_allow_thirteen_seconds_for_host_shutdown() => _shutdown.HostShutdownTimeout.ShouldEqual(TimeSpan.FromSeconds(13));
}
