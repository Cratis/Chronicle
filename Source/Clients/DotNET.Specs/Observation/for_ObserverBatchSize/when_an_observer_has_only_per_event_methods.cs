// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Observation.for_ObserverBatchSize;

public class when_an_observer_has_only_per_event_methods : Specification
{
    ObserverBatchSize _result;

    void Because() => _result = ObserverBatchSize.For(typeof(Observer));

    [Fact] void should_default_to_pages_of_one_hundred_events() => _result.MaxEvents.ShouldEqual(ObserverBatchSize.DefaultMaxEventsForPerEventMethods);
    [Fact] void should_leave_the_bytes_to_the_kernel() => _result.MaxBytes.ShouldEqual(0);
    [Fact] void should_not_wait_for_fuller_pages() => _result.MaxDelay.ShouldEqual(TimeSpan.Zero);
    [Fact] void should_default_the_concurrency_to_the_processor_count() => _result.MaxConcurrency.ShouldEqual(Environment.ProcessorCount);

    public record SomethingHappened(string Name);

    public class Observer
    {
        public Task Handle(SomethingHappened @event, EventContext context) => Task.CompletedTask;
    }
}
