// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Sequences;
using ProtoBuf;

namespace Cratis.Chronicle.EventSequences.for_AppendManyRequest;

public class when_round_tripping_through_protobuf_with_empty_optional_members : Specification
{
    AppendManyRequest _request;
    AppendManyRequest _result;

    void Establish() => _request = new()
    {
        EventStore = "EventStore",
        Namespace = "Namespace",
        EventSequenceId = "EventSequence",
        EventSourceId = "EventSource",
        CorrelationId = Guid.NewGuid(),
        CausedBy = new Identity
        {
            Subject = "Subject",
            Name = "Name",
            UserName = "UserName"
        },
        Causation =
        [
            new Causation
            {
                Occurred = DateTimeOffset.UtcNow,
                Type = "SomeCause"
            }
        ]
    };

    void Because() => _result = Serializer.DeepClone(_request);

    [Fact] void should_keep_events_non_null() => _result.Events.ShouldNotBeNull();
    [Fact] void should_keep_events_empty() => _result.Events.ShouldBeEmpty();
    [Fact] void should_keep_causation_non_null() => _result.Causation.ShouldNotBeNull();
    [Fact] void should_keep_the_causation_entry() => _result.Causation.Count().ShouldEqual(1);
    [Fact] void should_keep_nested_causation_properties_non_null() => _result.Causation.First().Properties.ShouldNotBeNull();
    [Fact] void should_keep_nested_causation_properties_empty() => _result.Causation.First().Properties.ShouldBeEmpty();
}
