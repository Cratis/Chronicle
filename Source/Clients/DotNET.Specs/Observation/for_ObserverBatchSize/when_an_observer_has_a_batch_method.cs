// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverBatchSize;

public class when_an_observer_has_a_batch_method : Specification
{
    ObserverBatchSize _result;

    void Because() => _result = ObserverBatchSize.For(typeof(Observer));

    [Fact] void should_default_to_pages_of_five_hundred_events() => _result.MaxEvents.ShouldEqual(ObserverBatchSize.DefaultMaxEventsForBatchMethods);
    [Fact] void should_not_wait_for_fuller_pages() => _result.MaxDelay.ShouldEqual(TimeSpan.Zero);

    public record SomethingHappened(string Name);

    public class Observer
    {
        [Observes<SomethingHappened>]
        public Task Handle(EventBatch batch, BatchContext context) => Task.CompletedTask;
    }
}
