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

    [Fact] void should_return_read_model_with_incremented_count() => ((ReadModel)_result.ReadModelState!).Count.ShouldEqual(_current.Count + 1);
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
}
