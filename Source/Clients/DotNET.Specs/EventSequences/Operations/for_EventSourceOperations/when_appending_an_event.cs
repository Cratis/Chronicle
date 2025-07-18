// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Operations.given;

namespace Cratis.Chronicle.EventSequences.Operations.for_EventSourceOperations;

public class when_appending_an_event : a_new_event_source_operations
{
    object _event;

    void Establish()
    {
        _event = "something happened";
    }

    void Because() => _operations.Append(_event);

    [Fact] void should_add_an_append_operation() => _operations.Operations.OfType<AppendOperation>().Count().ShouldEqual(1);
    [Fact] void should_hold_the_appended_event() => _operations.GetAppendedEvents().ShouldContain(_event);
}
