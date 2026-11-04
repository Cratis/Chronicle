// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Reactors.for_ReactorInvoker.when_discovering_batch_methods;

public class and_a_reactor_mixes_a_batch_method_and_a_per_event_method : given.a_registry
{
    IEnumerable<string> _eventTypeIds;

    void Because() => _eventTypeIds = ReactorInvoker.GetEventTypesFor(_eventTypes, typeof(Reactor)).Select(_ => _.Id.Value);

    [Fact] void should_subscribe_to_the_event_types_of_both() => _eventTypeIds.ShouldContainOnly("registered", "renamed", "removed");

    public class Reactor : IReactor
    {
        [Observes<Registered>]
        [Observes<Renamed>]
        public Task Index(EventBatch batch, BatchContext context) => Task.CompletedTask;

        public Task Removed(Removed @event) => Task.CompletedTask;
    }
}
