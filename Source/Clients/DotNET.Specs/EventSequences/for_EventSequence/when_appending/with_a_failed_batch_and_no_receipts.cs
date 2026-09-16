// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class with_a_failed_batch_and_no_receipts : given.an_acknowledged_append
{
    AppendManyResult _result;

    void Establish()
    {
        _batchResponse.Errors = ["rejected"];
        _batchResponse.Receipts = [];
        _batchResponse.SequenceNumbers = [];
    }

    async Task Because() => _result = await _eventSequence.AppendMany(
        [
            new(_source, "first") { Subject = "first-subject", Tags = ["first-tag"], Occurred = _occurred, EventSourceType = "request-source", EventStreamType = "request-stream", EventStreamId = "request-id" },
            new(_source, "second") { Subject = "second-subject", Tags = ["second-tag"] }
        ],
        correlationId: _correlationId);

    [Fact] void should_return_the_failure() => _result.HasErrors.ShouldBeTrue();
    [Fact] void should_have_no_receipts() => _result.Receipts.ShouldBeEmpty();
    [Fact] void should_notify_for_each_attempt() => _notifications.Length.ShouldEqual(2);
    [Fact] void should_preserve_failures_on_notifications() => _notifications.All(_ => _.Result.HasErrors).ShouldBeTrue();
    [Fact] void should_not_claim_persisted_sequences() => _notifications.All(_ => _.Event.Context.SequenceNumber == EventSequenceNumber.Unavailable).ShouldBeTrue();
    [Fact] void should_not_fabricate_receipts() => _notifications.All(_ => _.Result.Receipt is null).ShouldBeTrue();
    [Fact] void should_preserve_request_subjects() => _notifications.Select(_ => _.Event.Context.Subject.Value).ShouldEqual(["first-subject", "second-subject"]);
    [Fact] void should_preserve_request_tags() => _notifications.SelectMany(_ => _.Event.Context.Tags).Select(_ => _.Value).ShouldEqual(["first-tag", "second-tag"]);
    [Fact] void should_preserve_request_source_type() => _notifications[0].Event.Context.EventSourceType.Value.ShouldEqual("request-source");
    [Fact] void should_preserve_request_stream_type() => _notifications[0].Event.Context.EventStreamType.Value.ShouldEqual("request-stream");
    [Fact] void should_preserve_request_stream_id() => _notifications[0].Event.Context.EventStreamId.Value.ShouldEqual("request-id");
    [Fact] void should_preserve_request_timestamp() => _notifications[0].Event.Context.Occurred.ShouldEqual(_occurred);
    [Fact] void should_not_invent_a_timestamp() => _notifications[^1].Event.Context.Occurred.ShouldEqual(DateTimeOffset.MinValue);
    [Fact] void should_preserve_request_causation() => _notifications.All(_ => _.Event.Context.Causation.SequenceEqual(_currentCausationChain)).ShouldBeTrue();
    [Fact] void should_preserve_request_identity() => _notifications.All(_ => _.Event.Context.CausedBy.Subject == "caller").ShouldBeTrue();
    [Fact] void should_preserve_request_correlation() => _notifications.All(_ => _.Event.Context.CorrelationId == _correlationId).ShouldBeTrue();
}
