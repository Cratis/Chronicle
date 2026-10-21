// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ShutdownDrainBudget.when_computing_effective_timeout;

public class and_the_configured_budget_is_shorter : Specification
{
    TimeSpan _effective;

    void Because() => _effective = ShutdownDrainBudget.EffectiveTimeout(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(10));

    [Fact] void should_use_the_configured_budget() => _effective.ShouldEqual(TimeSpan.FromSeconds(4));
}
