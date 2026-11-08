// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Reactors.for_ReactorInvoker.when_discovering_batch_methods;

public class and_a_batch_method_takes_a_batch_context_a_cancellation_token_and_a_service : given.a_registry
{
    IEnumerable<EventType> _eventTypesObserved;

    void Because() => _eventTypesObserved = ReactorInvoker.GetEventTypesFor(_eventTypes, typeof(Reactor));

    [Fact] void should_observe_the_event_type() => _eventTypesObserved.Select(_ => _.Id.Value).ShouldContainOnly("registered");

    public class Reactor : IReactor
    {
        [Observes<Registered>]
        public Task Index(EventBatch batch, BatchContext context, IServiceProvider services, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
