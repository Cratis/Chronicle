// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.for_AccumulatorRedeliverySafety.when_the_kernel_crashes_during_catch_up.and_the_accumulators_are_event_source_keyed.context;

namespace Cratis.Chronicle.Integration.for_AccumulatorRedeliverySafety.when_the_kernel_crashes_during_catch_up;

/// <summary>
/// The oracle for the accumulator redelivery hazard: an event redelivered because the job step resumed from a
/// debounced checkpoint must leave an accumulating projection and a reducer fold untouched.
/// </summary>
/// <param name="context">The scenario context.</param>
[Collection(ChronicleCollection.Name)]
public class and_the_accumulators_are_event_source_keyed(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture)
        : given.accumulators_catching_up_across_a_kernel_crash(chronicleFixture);

    [Fact] void should_have_redelivered_at_least_one_event() => Context.RedeliveredSequenceNumbers.ShouldBeGreaterThan(0);
    [Fact] void should_count_every_event_exactly_once() => Context.CountedResult.Handled.ShouldEqual(context.EventCount);
    [Fact] void should_total_every_event_exactly_once() => Context.TotalResult.Total.ShouldEqual(context.EventCount);
}
