// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Idempotency;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_idempotency_key;

public class and_the_key_is_new : given.an_event_sequence_with_idempotency_keys
{
    async Task Because() => _result = await AppendKeyed();
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_report_appended() => _result.Outcome.ShouldEqual(AppendOutcome.Committed);
    [Fact] void should_store_the_namespaced_identity() => _written.Single().Identity!.Id.ShouldEqual("append-key:one");
    [Fact] void should_store_a_parseable_fingerprint() => StoredAppendFingerprint.Parse(_written.Single().Identity!.Fingerprint).IsSuccess.ShouldBeTrue();
    [Fact] async Task should_enqueue() => await _appendedEventsQueues.Received(1).Enqueue(Arg.Any<IEnumerable<Concepts.Events.AppendedEvent>>());
}
