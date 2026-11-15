// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Reactors.for_ReactorInvoker.when_discovering_batch_methods;

public class and_a_batch_method_takes_an_optional_cancellation_token : given.a_registry
{
    IEnumerable<EventType> _eventTypesObserved;

    void Because() => _eventTypesObserved = ReactorInvoker.GetEventTypesFor(_eventTypes, typeof(Reactor));

    [Fact] void should_accept_the_reactor() => _eventTypesObserved.Select(_ => _.Id.Value).ShouldContainOnly("registered");

    public class Reactor : IReactor
    {
        [Observes<Registered>]
        public Task Index(EventBatch batch, BatchContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
