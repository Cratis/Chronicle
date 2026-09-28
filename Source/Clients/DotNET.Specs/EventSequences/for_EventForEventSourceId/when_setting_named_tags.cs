// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventForEventSourceId;

public class when_setting_named_tags : Specification
{
    EventForEventSourceId _event;
    List<NamedTag> _input;

    void Establish()
    {
        _input = [new("key", "value")];
        _event = new(EventSourceId.New(), "event") { NamedTags = _input };
    }

    void Because() => _input.Add(new NamedTag("another", "value"));

    [Fact] void should_snapshot_the_input() => _event.NamedTags.Select(_ => _.Name.Value).ShouldEqual(["key"]);
}
