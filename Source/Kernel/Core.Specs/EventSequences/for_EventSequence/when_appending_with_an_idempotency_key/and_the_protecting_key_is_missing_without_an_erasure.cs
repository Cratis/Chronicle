// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_idempotency_key;

public class and_the_protecting_key_is_missing_without_an_erasure : given.an_event_sequence_with_idempotency_keys
{
    async Task Establish()
    {
        await Protect();
        await _backingKeys.DeleteFor(EventStore, EventStoreNamespace, _eventSourceId.Value);
    }
    async Task Because() => _result = await AppendKeyed();
    [Fact] void should_report_unverifiable() => _result.Conflicts.Single().Reason.ShouldEqual(IdempotencyConflictReason.Unverifiable);
    [Fact] void should_name_pii_in_the_exact_refusal() => _result.Result.Errors.Single().Value.ShouldEqual("IdempotencyKeyUnverifiable: Idempotency key 'one' already appended event type 'some-event' at sequence number 0, but the key that protected its pii values is not in key storage and no erasure covers it, so this retry cannot be compared. Nothing was written. Restore the key from a backup, or, for personal data that is meant to be gone, erase the subject; then recover the partition.");
    [Fact] async Task should_not_write() => await ShouldNotWrite();
}
