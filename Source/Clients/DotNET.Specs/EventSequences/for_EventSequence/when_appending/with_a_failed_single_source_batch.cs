// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class with_a_failed_single_source_batch : given.an_acknowledged_append
{
    AppendManyResult _result;

    void Establish()
    {
        _batchResponse.Errors = ["rejected"];
        _batchResponse.Receipts = [];
        _batchResponse.SequenceNumbers = [];
    }

    async Task Because() => _result = await _eventSequence.AppendMany(_source, ["first", "second"], correlationId: _correlationId, tags: ["request-tag"], occurred: _occurred, subject: "request-subject");

    [Fact] void should_return_the_failure() => _result.HasErrors.ShouldBeTrue();
    [Fact] void should_notify_each_attempt() => _notifications.Length.ShouldEqual(2);
    [Fact] void should_preserve_failure_results() => _notifications.All(_ => _.Result.HasErrors).ShouldBeTrue();
    [Fact] void should_not_fabricate_receipts() => _notifications.All(_ => _.Result.Receipt is null).ShouldBeTrue();
    [Fact] void should_not_claim_persisted_sequences() => _notifications.All(_ => _.Event.Context.SequenceNumber == EventSequenceNumber.Unavailable).ShouldBeTrue();
    [Fact] void should_preserve_request_subjects() => _notifications.Select(_ => _.Event.Context.Subject.Value).ShouldEqual(["request-subject", "request-subject"]);
    [Fact] void should_preserve_request_tags() => _notifications.SelectMany(_ => _.Event.Context.Tags).Select(_ => _.Value).ShouldEqual(["request-tag", "request-tag"]);
    [Fact] void should_preserve_request_timestamps() => _notifications.All(_ => _.Event.Context.Occurred == _occurred).ShouldBeTrue();
    [Fact] void should_preserve_request_causation() => _notifications.All(_ => _.Event.Context.Causation.SequenceEqual(_currentCausationChain)).ShouldBeTrue();
    [Fact] void should_preserve_request_identity() => _notifications.All(_ => _.Event.Context.CausedBy.Subject == "caller").ShouldBeTrue();
    [Fact] void should_preserve_request_correlation() => _notifications.All(_ => _.Event.Context.CorrelationId == _correlationId).ShouldBeTrue();
}
