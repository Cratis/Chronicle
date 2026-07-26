// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.for_AccumulatorRedeliverySafety.when_the_kernel_crashes_during_catch_up.and_the_projection_collapses_event_sources.context;

namespace Cratis.Chronicle.Integration.for_AccumulatorRedeliverySafety.when_the_kernel_crashes_during_catch_up;

/// <summary>
/// The regression test for the reason the guard is scoped. A projection that collapses every event source onto one
/// document is written out of order by design, so the guard must not engage for it. If it ever did, this document
/// would silently lose events — under-counting, which is worse than the double-counting the guard exists to stop,
/// because it would break projections that work correctly today.
/// </summary>
/// <param name="context">The scenario context.</param>
[Collection(ChronicleCollection.Name)]
public class and_the_projection_collapses_event_sources(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture)
        : given.accumulators_catching_up_across_a_kernel_crash(chronicleFixture);

    [Fact] void should_have_redelivered_at_least_one_event() => Context.RedeliveredSequenceNumbers.ShouldBeGreaterThan(0);
    [Fact] void should_not_drop_any_event() => Context.CollapsedResult.Handled.ShouldBeGreaterThan(context.EventCount - 1);
}
