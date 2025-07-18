// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Operations.given;

namespace Cratis.Chronicle.EventSequences.Operations.for_EventSourceOperations;

public class when_getting_operations_of_type : a_new_event_source_operations
{
    object _firstEvent;
    object _secondEvent;

    void Establish()
    {
        _firstEvent = "first";
        _secondEvent = "second";
        _operations
            .Append(_firstEvent)
            .Append(_secondEvent);
    }

    IEnumerable<AppendOperation> _result;
    void Because() => _result = _operations.GetOperationsOfType<AppendOperation>();

    [Fact] void should_return_all_append_operations() => _result.Count().ShouldEqual(2);
    [Fact] void should_include_first_event() => _result.Select(_ => _.Event).ShouldContain(_firstEvent);
    [Fact] void should_include_second_event() => _result.Select(_ => _.Event).ShouldContain(_secondEvent);
}
