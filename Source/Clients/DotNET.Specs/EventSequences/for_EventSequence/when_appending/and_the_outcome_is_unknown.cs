// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class and_the_outcome_is_unknown : given.an_event_sequence
{
    readonly Guid _correlationId = Guid.NewGuid();
    AppendResult _single;
    AppendManyResult _batch;
    AppendManyResult _multiSource;

    void Establish()
    {
        _identityProvider.GetCurrent().Returns(Identities.Identity.System);
        _eventTypes.HasFor(typeof(string)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(string)).Returns(new EventType("event", EventTypeGeneration.First));
        _eventSerializer.Serialize(Arg.Any<string>()).Returns(new JsonObject());
        _sequences.Append(Arg.Any<Contracts.Sequences.AppendRequest>(), CallContext.Default)
            .Returns(CommandResult<Contracts.Sequences.AppendResponse>.Success(_correlationId, new()
            {
                CorrelationId = _correlationId,
                Errors = ["AppendOutcomeUnknown"]
            }));
        var batchResponse = CommandResult<Contracts.Sequences.AppendManyResponse>.Success(_correlationId, new()
        {
            CorrelationId = _correlationId,
            Errors = ["AppendOutcomeUnknown"]
        });
        _sequences.AppendMany(Arg.Any<Contracts.Sequences.AppendManyRequest>(), CallContext.Default).Returns(batchResponse);
        _sequences.AppendManyForEventSources(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesRequest>(), CallContext.Default).Returns(batchResponse);
    }

    async Task Because()
    {
        _single = await _eventSequence.Append("source", "event");
        _batch = await _eventSequence.AppendMany("source", ["event"]);
        _multiSource = await _eventSequence.AppendMany([new EventForEventSourceId("source", "event")]);
    }

    [Fact] void should_map_the_single_error() => _single.Errors.ShouldContainOnly([AppendError.AppendOutcomeUnknown]);
    [Fact] void should_map_the_batch_error() => _batch.Errors.ShouldContainOnly([AppendError.AppendOutcomeUnknown]);
    [Fact] void should_map_the_multi_source_error() => _multiSource.Errors.ShouldContainOnly([AppendError.AppendOutcomeUnknown]);
    [Fact] void should_not_claim_success() => _single.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_claim_batch_success() => _batch.IsSuccess.ShouldBeFalse();
    [Fact] void should_retain_the_correlation_id() => _batch.CorrelationId.Value.ShouldEqual(_correlationId);
    [Fact] void should_not_report_a_constraint_rejection() => _single.HasConstraintViolations.ShouldBeFalse();
}
