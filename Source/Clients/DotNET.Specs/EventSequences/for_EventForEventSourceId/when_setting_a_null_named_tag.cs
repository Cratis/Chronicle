// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventForEventSourceId;

public class when_setting_a_null_named_tag : Specification
{
    Exception _error;
    EventForEventSourceId _event;

    void Because() => _error = Catch.Exception(() => _event = new EventForEventSourceId(EventSourceId.New(), "event") { NamedTags = [null!] });

    [Fact] void should_reject_the_null_element() => _error.ShouldBeOfExactType<InvalidNamedTag>();
}
