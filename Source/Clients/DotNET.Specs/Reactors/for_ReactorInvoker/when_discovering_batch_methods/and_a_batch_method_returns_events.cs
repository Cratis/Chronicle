// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Reactors.for_ReactorInvoker.when_discovering_batch_methods;

public class and_a_batch_method_returns_events : given.a_registry
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => ReactorInvoker.GetEventTypesFor(_eventTypes, typeof(Reactor)));

    [Fact] void should_refuse_the_reactor() => _error.ShouldBeOfExactType<InvalidBatchMethodReturnType>();

    public class Reactor : IReactor
    {
        [Observes<Registered>]
        public Task<Renamed> Index(EventBatch batch) => Task.FromResult(new Renamed());
    }
}
