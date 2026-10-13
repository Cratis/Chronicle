// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ShutdownDrainBudget.when_computing_effective_timeout;

public class and_the_host_timeout_is_shorter : Specification
{
    TimeSpan _effective;

    void Because() => _effective = ShutdownDrainBudget.EffectiveTimeout(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(5));

    [Fact] void should_cap_the_shared_budget() => _effective.ShouldEqual(TimeSpan.FromSeconds(3));
}
