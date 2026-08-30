// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_ReducerInvoker.when_resolving_the_reducer_method;

/// <summary>
/// A helper extracted from a reducer method keeps the event and the current state as its parameters, which is
/// exactly the shape dispatch looks for. The public method has to keep the event type, or the reducer quietly
/// produces the helper's partial state instead of the state it was written to produce.
/// </summary>
public class and_a_private_helper_is_shaped_like_one : given.a_reducer_invoker_for<ReducerWithAPrivateHelper>
{
    ValidEvent _event;
    ReadModel _current;
    ReduceResult _result;

    void Establish()
    {
        _event = new();
        _current = new(5);
    }

    async Task Because() => _result = await _invoker.Invoke(_serviceProvider, [new(_event, EventContext.Empty)], _current);

    [Fact] void should_reduce_with_the_public_method() => ((ReadModel)_result.ReadModelState).Count.ShouldEqual(8);
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
}
