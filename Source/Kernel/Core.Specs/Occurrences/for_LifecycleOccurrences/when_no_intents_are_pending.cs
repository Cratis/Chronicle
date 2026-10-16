// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences.for_LifecycleOccurrences;

public class when_no_intents_are_pending : given.a_lifecycle_grain
{
    Task Because() => _silo.TimerRegistry.FireAllAsync();

    [Fact] void should_disarm_the_reconciliation_timer() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
