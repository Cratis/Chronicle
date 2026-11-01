// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

namespace Cratis.Chronicle.Setup.Serialization.for_AppendedEventSerializer;

public class when_a_message_shares_compact_references : given.a_compact_serializer
{
    List<object> _input;
    List<object> _result;

    void Establish()
    {
        var first = appended_event with { Content = ContentWith(("value", 42)) };
        var second = appended_event with { Content = ContentWith(("value", 84)) };
        _input = [first, "shared", first, "shared", second];
    }

    void Because()
    {
        var messageSerializer = service_provider.GetRequiredService<Serializer<List<object>>>();
        _result = messageSerializer.Deserialize(messageSerializer.SerializeToArray(_input))!;
    }

    [Fact] void should_preserve_the_first_event() => AssertEquivalent((AppendedEvent)_result[0], (AppendedEvent)_input[0]);
    [Fact] void should_preserve_the_repeated_event_reference() => _result[2].ShouldBeSame(_result[0]);
    [Fact] void should_preserve_the_following_string() => _result[3].ShouldEqual("shared");
    [Fact] void should_preserve_the_following_event() => AssertEquivalent((AppendedEvent)_result[4], (AppendedEvent)_input[4]);
}
