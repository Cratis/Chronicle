// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_ReducerInvoker.when_invoking_bulk_with_initial_state;

public class and_method_is_synchronous : given.a_reducer_invoker_for<SyncReducer>
{
    IEnumerable<EventAndContext> _eventsAndContexts;
    ReadModel _initial;
    ReduceResult _reduceResult;
    ReadModel _result;

    void Establish()
    {
        _initial = new(42);
        _eventsAndContexts =
        [
            new(new ValidEvent(), EventContext.Empty with { SequenceNumber = 0 }),
            new(new ValidEvent(), EventContext.Empty with { SequenceNumber = 1 }),
            new(new ValidEvent(), EventContext.Empty with { SequenceNumber = 2 }),
            new(new ValidEvent(), EventContext.Empty with { SequenceNumber = 3 })
        ];
    }

    async Task Because()
    {
        _reduceResult = (await _invoker.Invoke(_serviceProvider, _eventsAndContexts, _initial))!;
        _result = _reduceResult.ReadModelState as ReadModel;
    }

    [Fact] void should_return_a_read_model_that_has_been_iterated_on() => _result.Count.ShouldEqual(_eventsAndContexts.Count() + _initial.Count);
    [Fact] void should_be_successful() => _reduceResult.IsSuccess.ShouldBeTrue();
}
