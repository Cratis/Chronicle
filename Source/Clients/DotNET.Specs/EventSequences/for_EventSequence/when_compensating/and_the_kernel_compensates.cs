// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_compensating;

public class and_the_kernel_compensates : given.an_event_sequence
{
    Contracts.Sequences.CompensateRequest _request;
    AppendResult _result;

    void Establish()
    {
        _eventTypes.HasFor(typeof(string)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(string)).Returns(new EventType("compensating-event", EventTypeGeneration.First));
        _eventSerializer.Serialize(Arg.Any<string>()).Returns(new JsonObject());
        _identityProvider.GetCurrent().Returns(Identity.NotSet);
        _sequences.When(_ => _.Compensate(Arg.Any<Contracts.Sequences.CompensateRequest>(), CallContext.Default))
            .Do(call => _request = call.Arg<Contracts.Sequences.CompensateRequest>());
        _sequences.Compensate(Arg.Any<Contracts.Sequences.CompensateRequest>(), CallContext.Default)
            .Returns(CommandResult<Contracts.Sequences.AppendResponse>.Success(Guid.NewGuid(), new Contracts.Sequences.AppendResponse
            {
                SequenceNumber = 9,
                ConstraintViolations = [],
                Errors = []
            }));
    }

    async Task Because() => _result = await _eventSequence.Compensate(new EventSequenceNumber(3), "reversal", tags: ["plain"]);

    [Fact] void should_send_the_compensated_sequence_number() => _request.CompensationFor.ShouldEqual(3UL);
    [Fact] void should_send_the_event_type() => _request.EventType.Id.ShouldEqual("compensating-event");
    [Fact] void should_send_the_serialized_content() => _request.Content.ShouldEqual("{}");
    [Fact] void should_send_the_sequence_identifier() => _request.EventSequenceId.ShouldEqual(_eventSequence.Id.Value);
    [Fact] void should_send_the_tags() => _request.Tags.ShouldContain("plain");
    [Fact] void should_send_the_identity() => _request.CausedBy.ShouldNotBeNull();
    [Fact] void should_return_the_sequence_number_of_the_compensation() => _result.SequenceNumber.ShouldEqual(new EventSequenceNumber(9));
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
}
