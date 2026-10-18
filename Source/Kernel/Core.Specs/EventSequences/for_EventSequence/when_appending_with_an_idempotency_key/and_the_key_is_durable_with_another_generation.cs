// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_idempotency_key;

public class and_the_key_is_durable_with_another_generation : given.an_event_sequence_with_idempotency_keys
{
    void Establish()
    {
        Durable();
        _request = _request with { EventType = new(_eventType.Id, 2) };
    }
    async Task Because() => _result = await AppendKeyed();
    [Fact] void should_name_the_original_generation() => _result.Conflicts.Single().StoredEventType.Generation.ShouldEqual(EventTypeGeneration.First);
    [Fact] void should_report_generation_conflict() => _result.Conflicts.Single().Reason.ShouldEqual(IdempotencyConflictReason.Generation);
    [Fact] void should_explain_how_to_retry() => _result.Result.Errors.Single().Value.ShouldEqual("IdempotencyKeyConflict: Idempotency key 'one' already appended event type 'some-event' generation 1 at sequence number 0; this append is generation 2. Keyed appends are not migrated between generations. To repeat the recorded effect, append generation 1; for a new effect, use a new idempotency key. Nothing was written.");
    [Fact] async Task should_not_write() => await ShouldNotWrite();
}
