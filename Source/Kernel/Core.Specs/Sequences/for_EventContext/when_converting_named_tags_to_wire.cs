// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Sequences.for_EventContext;

public class when_converting_named_tags_to_wire : Specification
{
    Contracts.Sequences.EventContext _result;

    void Because()
    {
        var context = Concepts.Events.EventContext.From(
            "store",
            "namespace",
            Concepts.Events.EventType.Unknown,
            EventSourceType.Default,
            "source",
            EventStreamType.All,
            EventStreamId.Default,
            EventSequenceNumber.First,
            CorrelationId.New()) with
        {
            NamedTags = [new Concepts.Events.NamedTag(new TagName("account"), "one")]
        };
        _result = context.ToApi().ToContract();
    }

    [Fact] void should_carry_name_and_value() => _result.NamedTags.Single().Value.ShouldEqual("one");
}
