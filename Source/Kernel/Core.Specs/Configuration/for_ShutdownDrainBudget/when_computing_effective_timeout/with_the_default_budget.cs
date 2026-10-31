// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ShutdownDrainBudget.when_computing_effective_timeout;

public class with_the_default_budget : Specification
{
    TimeSpan _effective;

    void Because() => _effective = ShutdownDrainBudget.EffectiveTimeout(new ChronicleOptions().Shutdown.DrainTimeout, TimeSpan.FromSeconds(10));

    [Fact] void should_reserve_two_seconds_for_host_shutdown() => _effective.ShouldEqual(TimeSpan.FromSeconds(8));
}
