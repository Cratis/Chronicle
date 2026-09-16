// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

public class and_storage_acknowledges_event_metadata : given.an_event_sequence
{
    EventContext _storedContext;
    AppendResult _result;

    void Establish()
    {
        _storedContext = EventContext.From(
            EventStore,
            EventStoreNamespace,
            _eventType,
            "Account",
            _eventSourceId,
            "Payments",
            "period",
            EventSequenceNumber.First,
            CorrelationId.NotSet,
            [(Tag)"persisted-tag"],
            new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
            (Subject)"subject") with
        {
            Causation = [new(new DateTimeOffset(2019, 1, 1, 0, 0, 0, TimeSpan.Zero), "stored-cause", new Dictionary<string, string> { ["origin"] = "kernel" })],
            CausedBy = new("operator", "Operator", "operator", new Identity("upstream", "Upstream"))
        };
        _eventSequenceStorage.Append(default!, default!, default!, default!, default!, default!, default!, default!, default!, default!, default, default!, default!, default)
            .ReturnsForAnyArgs(Task.FromResult<Result<AppendedEvent, DuplicateEventSequenceNumber>>(new AppendedEvent(_storedContext, new ExpandoObject())));
    }

    async Task Because() => _result = await _eventSequence.Append(EventSourceType.Default, _eventSourceId, EventStreamType.All, EventStreamId.Default, _eventType, new JsonObject(), CorrelationId.NotSet, [], Identity.NotSet, [], ConcurrencyScope.None, occurred: null, subject: null, includeReceipt: true);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_include_a_receipt() => _result.Receipt.ShouldNotBeNull();
    [Fact] void should_report_the_stored_source_type() => _result.Receipt!.EventSourceType.ShouldEqual(_storedContext.EventSourceType);
    [Fact] void should_report_the_stored_stream_type() => _result.Receipt!.EventStreamType.ShouldEqual(_storedContext.EventStreamType);
    [Fact] void should_report_the_stored_stream_id() => _result.Receipt!.EventStreamId.ShouldEqual(_storedContext.EventStreamId);
    [Fact] void should_report_the_stored_occurrence() => _result.Receipt!.Occurred.ShouldEqual(_storedContext.Occurred);
    [Fact] void should_report_the_stored_subject() => _result.Receipt!.Subject.ShouldEqual(_storedContext.Subject);
    [Fact] void should_report_the_stored_tags() => _result.Receipt!.Tags.ShouldEqual(_storedContext.Tags);
    [Fact] void should_pair_the_receipt_with_the_sequence_number() => _result.Receipt!.SequenceNumber.ShouldEqual(_result.SequenceNumber);
    [Fact] void should_report_the_stored_causation() => _result.Receipt!.Causation.Single().Type.ShouldEqual("stored-cause");
    [Fact] void should_report_the_stored_identity_chain() => _result.Receipt!.CausedBy.OnBehalfOf!.Subject.ShouldEqual("upstream");
    [Fact] void should_report_the_stored_observation_state() => _result.Receipt!.ObservationState.ShouldEqual(_storedContext.ObservationState);
    [Fact] void should_preserve_the_receipt_while_reporting_concurrency() => _result.ReportingConcurrencyCheck(true).Receipt.ShouldEqual(_result.Receipt);
}
