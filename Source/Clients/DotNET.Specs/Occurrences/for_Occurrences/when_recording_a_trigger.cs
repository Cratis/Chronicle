// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences.given;

namespace Cratis.Chronicle.Occurrences.for_Occurrences;

public class when_recording_a_trigger : an_occurrence_client
{
    Task Establish() => _occurrences.Discover();

    async Task Because() => _result = await _occurrences.Record(new TestTrigger("hello"), "request-1");

    [Fact] void should_bind_the_namespace() => _record.Namespace.ShouldEqual("tenant");
    [Fact] void should_bind_the_event_store() => _record.EventStore.ShouldEqual("store");
    [Fact] void should_use_the_registered_identity() => _record.Trigger.ShouldEqual("test-trigger");
    [Fact] void should_preserve_the_idempotency_identity() => _record.IdempotencyId.ShouldEqual("request-1");
    [Fact] void should_serialize_using_the_client_options() => _record.Payload.ShouldEqual("{\"value\":\"hello\"}");
    [Fact] void should_return_the_durable_outcome() => _result.Status.ShouldEqual(OccurrenceRecordStatus.Recorded);
}
