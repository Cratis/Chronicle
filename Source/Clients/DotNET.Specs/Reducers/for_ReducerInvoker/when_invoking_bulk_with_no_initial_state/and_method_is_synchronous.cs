// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_ReducerInvoker.when_invoking_bulk_with_no_initial_state;

public class and_method_is_synchronous : given.a_reducer_invoker_for<SyncReducer>
{
    IEnumerable<EventAndContext> _eventsAndContexts;
    ReduceResult _reduceResult;
    ReadModel _result;

    void Establish()
    {
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
        _reduceResult = (await _invoker.Invoke(_serviceProvider, _eventsAndContexts, null))!;
        _result = _reduceResult.ReadModelState as ReadModel;
    }

    [Fact] void should_only_create_one_instance_of_the_reducer() => _serviceProvider.Received(1).GetService(typeof(SyncReducer));
    [Fact] void should_pass_the_events_and_contexts() => _reducer.ReceivedEventsAndContexts.ShouldEqual(_eventsAndContexts);
    [Fact] void should_return_a_read_model_that_has_been_iterated_on() => _result.Count.ShouldEqual(_eventsAndContexts.Count());
}
