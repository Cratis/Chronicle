// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

#nullable enable

namespace Cratis.Chronicle.Reducers.for_ReducerInvoker.when_invoking_and_reducer_returns_null;

public class with_asynchronous_nullable_reducer : given.a_reducer_invoker_for<AsyncNullableReducer>
{
    IEnumerable<EventAndContext> _eventsAndContexts;
    ReduceResult _reduceResult;

    void Establish()
    {
        _eventsAndContexts =
        [
            new(new ValidEvent(), EventContext.Empty with { SequenceNumber = 0 })
        ];
    }

    async Task Because()
    {
        _reduceResult = await _invoker.Invoke(_serviceProvider, _eventsAndContexts, new ReadModel());
    }

    [Fact] void should_return_null_as_model_state() => _reduceResult.ModelState.ShouldBeNull();
    [Fact] void should_be_successful() => _reduceResult.IsSuccess.ShouldBeTrue();
}