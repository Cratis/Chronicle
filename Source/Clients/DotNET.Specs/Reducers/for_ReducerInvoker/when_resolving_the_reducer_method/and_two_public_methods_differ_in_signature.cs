// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_ReducerInvoker.when_resolving_the_reducer_method;

/// <summary>
/// The richest signature wins: the method that asked for the event context is the more specific one, and it is
/// the one the author meant to run.
/// </summary>
public class and_two_public_methods_differ_in_signature : given.a_reducer_invoker_for<ReducerWithTwoMethodSignatures>
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

    [Fact] void should_reduce_with_the_method_taking_the_event_context() => ((ReadModel)_result.ReadModelState).Count.ShouldEqual(15);
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
}
