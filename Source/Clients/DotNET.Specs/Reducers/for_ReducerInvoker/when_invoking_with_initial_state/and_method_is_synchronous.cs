// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_ReducerInvoker.when_invoking_with_initial_state;

public class and_method_is_synchronous : given.a_reducer_invoker_for<SyncReducer>
{
    ValidEvent _event;
    EventContext _eventContext;
    ReadModel _current;
    ReduceResult _result;

    void Establish()
    {
        _event = new();
        _current = new();
        _eventContext = EventContext.Empty;
    }

    async Task Because() => _result = await _invoker.Invoke(_serviceProvider, [new(_event, _eventContext)], _current);

    [Fact] void should_pass_the_event() => _reducer.ReceivedEvents.First().ShouldEqual(_event);
    [Fact] void should_pass_current_read_model() => _reducer.ReceivedReadModels.First().ShouldEqual(_current);
    [Fact] void should_pass_the_event_context() => _reducer.ReceivedEventContexts.First().ShouldEqual(_eventContext);
    [Fact] void should_return_the_read_model() => _result.ReadModelState.ShouldEqual(_reducer.ReturnedReadModel);
}
