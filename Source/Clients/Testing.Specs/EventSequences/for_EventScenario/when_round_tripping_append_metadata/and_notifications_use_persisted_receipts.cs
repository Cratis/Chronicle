// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_round_tripping_append_metadata;

public class and_notifications_use_persisted_receipts : Specification, IDisposable
{
    EventScenario _scenario;
    AppendManyResult _result;
    IImmutableList<AppendedEvent> _stored;
    AppendedEventWithResult[] _notifications;
    IDisposable _subscription;

    void Establish()
    {
        _scenario = new EventScenario();
        _subscription = _scenario.EventLog.AppendOperations.Subscribe(events => _notifications = events.ToArray());
    }

    async Task Because()
    {
        _result = await _scenario.EventLog.AppendMany([
            new("first-source", new TestEvent("first")) { Tags = ["first-tag"], Subject = "first-subject" },
            new("second-source", new TestEvent("second")) { Tags = ["second-tag"], Subject = "second-subject" }
        ]);
        _stored = await _scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
    }

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] void should_have_two_receipts() => _result.Receipts.Count.ShouldEqual(2);
    [Fact] void should_notify_with_the_receipts() => _notifications.Select(_ => _.Event.Context).ShouldEqual(_result.Receipts);
    [Fact] void should_match_the_stored_sequence_order() => _result.Receipts.Select(_ => _.SequenceNumber).ShouldEqual(_stored.Select(_ => _.Context.SequenceNumber));
    [Fact] void should_match_the_stored_sources() => _result.Receipts.Select(_ => _.EventSourceId).ShouldEqual(_stored.Select(_ => _.Context.EventSourceId));
    [Fact] void should_match_the_stored_source_types() => _result.Receipts.Select(_ => _.EventSourceType).ShouldEqual(_stored.Select(_ => _.Context.EventSourceType));
    [Fact] void should_match_the_stored_stream_types() => _result.Receipts.Select(_ => _.EventStreamType).ShouldEqual(_stored.Select(_ => _.Context.EventStreamType));
    [Fact] void should_match_the_stored_stream_ids() => _result.Receipts.Select(_ => _.EventStreamId).ShouldEqual(_stored.Select(_ => _.Context.EventStreamId));
    [Fact] void should_match_the_stored_timestamps() => _result.Receipts.Select(_ => _.Occurred).ShouldEqual(_stored.Select(_ => _.Context.Occurred));
    [Fact] void should_match_the_stored_subjects() => _result.Receipts.Select(_ => _.Subject).ShouldEqual(_stored.Select(_ => _.Context.Subject));
    [Fact] void should_match_the_stored_tags() => _result.Receipts.SelectMany(_ => _.Tags).ShouldEqual(_stored.SelectMany(_ => _.Context.Tags));
    [Fact] void should_match_the_stored_hashes() => _result.Receipts.Select(_ => _.Hash).ShouldEqual(_stored.Select(_ => _.Context.Hash));
    [Fact] void should_match_the_stored_event_types() => _result.Receipts.Select(_ => _.EventType).ShouldEqual(_stored.Select(_ => _.Context.EventType));
    [Fact] void should_match_the_stored_stores() => _result.Receipts.Select(_ => _.EventStore).ShouldEqual(_stored.Select(_ => _.Context.EventStore));
    [Fact] void should_match_the_stored_namespaces() => _result.Receipts.Select(_ => _.Namespace).ShouldEqual(_stored.Select(_ => _.Context.Namespace));
    [Fact] void should_match_the_stored_correlations() => _result.Receipts.Select(_ => _.CorrelationId).ShouldEqual(_stored.Select(_ => _.Context.CorrelationId));
    [Fact] void should_match_the_stored_causation_types() => _result.Receipts.SelectMany(_ => _.Causation).Select(_ => _.Type).ShouldEqual(_stored.SelectMany(_ => _.Context.Causation).Select(_ => _.Type));
    [Fact] void should_match_the_stored_causation_timestamps() => _result.Receipts.SelectMany(_ => _.Causation).Select(_ => _.Occurred).ShouldEqual(_stored.SelectMany(_ => _.Context.Causation).Select(_ => _.Occurred));
    [Fact] void should_match_the_stored_causation_properties() => _result.Receipts.SelectMany(_ => _.Causation).SelectMany(_ => _.Properties).ShouldEqual(_stored.SelectMany(_ => _.Context.Causation).SelectMany(_ => _.Properties));
    [Fact] void should_match_the_stored_identity_chains() => _result.Receipts.Select(_ => _.CausedBy).ShouldEqual(_stored.Select(_ => _.Context.CausedBy));
    [Fact] void should_match_the_stored_observation_states() => _result.Receipts.Select(_ => _.ObservationState).ShouldEqual(_stored.Select(_ => _.Context.ObservationState));

    public void Dispose()
    {
        _subscription.Dispose();
        _scenario.Dispose();
    }
}
