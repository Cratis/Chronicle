// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_from_sequence_number_with_erased_properties;

public class with_filters_and_metadata : given.an_event_sequence
{
    Contracts.Sequences.FromSequenceNumberWithErasedPropertiesRequest _request;
    IImmutableList<AppendedEvent> _result;

    void Establish()
    {
        var type = new EventType("display", 1);
        _eventTypes.HasFor(type.Id).Returns(true);
        _eventTypes.GetClrTypeFor(type.Id).Returns(typeof(Display));
        _sequences.FromSequenceNumberWithErasedProperties(Arg.Any<Contracts.Sequences.FromSequenceNumberWithErasedPropertiesRequest>(), Arg.Any<CallContext>()).Returns(call =>
        {
            _request = call.Arg<Contracts.Sequences.FromSequenceNumberWithErasedPropertiesRequest>();
            return QueryResult<IEnumerable<Contracts.Sequences.AppendedEventResponse>>.Success(Guid.NewGuid(), [new()
            {
                Context = new()
                {
                    EventType = type.ToSequencesContract(), SequenceNumber = 42, EventSourceId = "source",
                    EventSourceType = EventSourceType.Default, EventStreamType = EventStreamType.All,
                    EventStreamId = EventStreamId.Default, CorrelationId = Guid.NewGuid(),
                    Causation = [], CausedBy = new(), Tags = [], Subject = "subject"
                },
                Content = "{\"Name\":\"\"}", ErasedProperties = ["contacts[0].email", "name"]
            }]);
        });
    }

    async Task Because() => _result = await _eventSequence.GetFromSequenceNumberWithErasedProperties(42, "source", [new("display", 1)]);

    [Fact] void should_forward_the_sequence_number() => _request.FromEventSequenceNumber.ShouldEqual(42UL);
    [Fact] void should_forward_the_source_filter() => _request.EventSourceId.ShouldEqual("source");
    [Fact] void should_forward_the_type_filter() => _request.EventTypeIds.ShouldEqual("display");
    [Fact] void should_copy_paths() => _result.Single().ErasedProperties.ShouldEqual<IEnumerable<string>>(["contacts[0].email", "name"]);
    [Fact] void should_preserve_subject() => _result.Single().Context.Subject.Value.ShouldEqual("subject");
    [Fact] void should_preserve_deserialized_content() => ((Display)_result.Single().Content).Name.ShouldEqual(string.Empty);
    [Fact] void should_not_use_the_legacy_wire_method() => _sequences.DidNotReceive().FromSequenceNumber(Arg.Any<Contracts.Sequences.FromSequenceNumberRequest>(), Arg.Any<CallContext>());

    record Display(string Name);
}
