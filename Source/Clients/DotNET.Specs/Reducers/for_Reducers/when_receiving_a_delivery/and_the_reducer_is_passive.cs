// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Receivers;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_receiving_a_delivery;

public class and_the_reducer_is_passive : given.all_dependencies
{
    IReducerHandler _handler;
    Exception _error;

    void Establish()
    {
        _handler = Substitute.For<IReducerHandler>();
        _handler.Id.Returns((ReducerId)"passive");
        _handler.IsActive.Returns(false);
        _handlersByModelType[typeof(object)] = _handler;
    }

    async Task Because() => _error = await Catch.Exception(() => ((IReceiveReducerDeliveries)_reducers).Deliver("passive", new ReduceOperationMessage()));
    [Fact] void should_refuse_the_delivery() => _error.ShouldBeOfExactType<ReducerDoesNotExist>();
    [Fact] void should_not_invoke_the_handler() => _handler.DidNotReceive().OnNext(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<object?>(), Arg.Any<IServiceProvider>());
}
