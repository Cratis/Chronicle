// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class and_a_single_source_batch_has_explicit_metadata : given.an_event_sequence_with_metadata
{
    readonly ConcurrencyScope _scope = new(41UL, "source", "Payments", "September", "Account");

    async Task Because() => await _eventSequence.AppendMany(
        _source,
        ["first", "second"],
        "Payments",
        "September",
        "Account",
        _correlationId,
        ["batch-tag"],
        _scope,
        _occurred,
        (Subject)"subject");

    [Fact] void should_send_both_events() => _batchRequest.Events.Count().ShouldEqual(2);
    [Fact] void should_preserve_the_source_type() => _batchRequest.Events.Select(_ => _.EventSourceType).ShouldEqual(["Account", "Account"]);
    [Fact] void should_preserve_the_stream_type() => _batchRequest.Events.Select(_ => _.EventStreamType).ShouldEqual(["Payments", "Payments"]);
    [Fact] void should_preserve_the_stream_id() => _batchRequest.Events.Select(_ => _.EventStreamId).ShouldEqual(["September", "September"]);
    [Fact] void should_preserve_subjects() => _batchRequest.Events.Select(_ => _.Subject).ShouldEqual(["subject", "subject"]);
    [Fact] void should_preserve_the_timestamp() => _batchRequest.Events.All(_ => (DateTimeOffset?)_.Occurred == _occurred).ShouldBeTrue();
    [Fact] void should_preserve_batch_tags() => _batchRequest.Events.All(_ => _.Tags.SequenceEqual(["batch-tag"])).ShouldBeTrue();
    [Fact] void should_preserve_the_correlation() => _batchRequest.CorrelationId.ShouldEqual(_correlationId.Value);
    [Fact] void should_preserve_the_concurrency_scope() => _batchRequest.ConcurrencyScopes.Single().Scope.SequenceNumber.ShouldEqual(_scope.SequenceNumber.Value);
    [Fact] void should_preserve_the_concurrency_source() => _batchRequest.ConcurrencyScopes.Single().EventSourceId.ShouldEqual(_source.Value);
    [Fact] void should_not_resolve_a_replacement_scope() => _concurrencyScopeStrategy.DidNotReceiveWithAnyArgs().GetScope(default!, default, default, default, default);
    [Fact] void should_notify_for_both_events() => _notifications.Length.ShouldEqual(2);
    [Fact] void should_notify_with_the_sent_metadata() => _notifications.All(_ => _.Event.Context.EventSourceType.Value == "Account" && _.Event.Context.EventStreamType.Value == "Payments" && _.Event.Context.EventStreamId.Value == "September").ShouldBeTrue();
    [Fact] void should_preserve_the_causation() => _batchRequest.Causation.ToClient().ShouldEqual(_currentCausationChain);
}
