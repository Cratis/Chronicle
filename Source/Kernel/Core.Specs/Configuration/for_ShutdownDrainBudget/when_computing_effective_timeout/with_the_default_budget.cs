// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ShutdownDrainBudget.when_computing_effective_timeout;

public class with_the_default_budget : Specification
{
    TimeSpan _effective;

    void Because()
    {
        var shutdown = new Shutdown();
        _effective = ShutdownDrainBudget.EffectiveTimeout(shutdown.DrainTimeout, shutdown.HostShutdownTimeout, shutdown.HostShutdownReserve);
    }

    [Fact] void should_preserve_the_eight_second_drain() => _effective.ShouldEqual(TimeSpan.FromSeconds(8));
}
