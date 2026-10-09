// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Projections.for_ProjectionFuturesStorage;

public class when_round_tripping_full_event_context : given.a_futures_storage
{
    EventContext _read;

    async Task Because()
    {
        await _storage.Save(_future.ProjectionId, _future);
        var reloadedStorage = new ProjectionFuturesStorage(_eventStore, _namespace, _database, _options);
        _read = (await reloadedStorage.GetForProjection(_future.ProjectionId)).Single().Event.Context;
    }

    [Fact] void should_preserve_the_event_type() => _read.EventType.ShouldEqual(new EventType("ChildCreated", 3, true));
    [Fact] void should_preserve_the_source_type() => _read.EventSourceType.ShouldEqual(new EventSourceType("order"));
    [Fact] void should_preserve_the_source_id() => _read.EventSourceId.ShouldEqual(new EventSourceId("child-42"));
    [Fact] void should_preserve_the_stream_type() => _read.EventStreamType.ShouldEqual(new EventStreamType("fulfillment"));
    [Fact] void should_preserve_the_stream_id() => _read.EventStreamId.ShouldEqual(new EventStreamId("shipment-7"));
    [Fact] void should_preserve_the_sequence_number() => _read.SequenceNumber.ShouldEqual(new EventSequenceNumber(42));
    [Fact] void should_preserve_the_occurred_time() => _read.Occurred.ShouldEqual(DateTimeOffset.Parse("2026-05-12T13:14:15.1234567+02:00"));
    [Fact] void should_preserve_the_occurred_offset() => _read.Occurred.Offset.ShouldEqual(TimeSpan.FromHours(2));
    [Fact] void should_preserve_the_correlation_id() => _read.CorrelationId.ShouldEqual(new CorrelationId(Guid.Parse("de001d50-d624-4d65-a550-314241d40e07")));
    [Fact] void should_preserve_the_causation_time() => _read.Causation.Single().Occurred.ShouldEqual(DateTimeOffset.Parse("2026-05-12T12:00:00+02:00"));
    [Fact] void should_preserve_the_causation_type() => _read.Causation.Single().Type.ShouldEqual(new CausationType("command"));
    [Fact] void should_preserve_the_causation_properties() => _read.Causation.Single().Properties["name"].ShouldEqual("CreateChild");
    [Fact] void should_preserve_the_identity_chain() => _read.CausedBy.ShouldEqual(new Identity("user-7", "Alice", "alice", new Identity("service-9", "Importer", "importer")));
    [Fact] void should_preserve_the_tags() => _read.Tags.ShouldContainOnly(new Tag("priority"), new Tag("imported"));
    [Fact] void should_preserve_the_hash() => _read.Hash.ShouldEqual(new EventHash("content-hash"));
    [Fact] void should_preserve_the_subject() => _read.Subject.ShouldEqual(new Subject("person-17"));
    [Fact] void should_preserve_the_observation_state() => _read.ObservationState.ShouldEqual(EventObservationState.Replay);
    [Fact] void should_preserve_the_event_source_name() => _read.EventSource.ShouldEqual(new EventSourceName("Orders"));
    [Fact] void should_preserve_the_named_tags() => _read.NamedTags.ShouldContainOnly(new NamedTag(new TagName("region"), "north"));
    [Fact] void should_preserve_the_event_store() => _read.EventStore.Value.ShouldEqual("orders");
    [Fact] void should_preserve_the_namespace() => _read.Namespace.Value.ShouldEqual("tenant-7");
}
