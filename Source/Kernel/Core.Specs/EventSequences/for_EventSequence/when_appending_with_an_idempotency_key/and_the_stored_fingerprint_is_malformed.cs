// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_idempotency_key;

public class and_the_stored_fingerprint_is_malformed : given.an_event_sequence_with_idempotency_keys
{
    void Establish() => Durable(fingerprint: "not a fingerprint");
    async Task Because() => _result = await AppendKeyed();
    [Fact] void should_refuse_comparison() => _result.Result.Errors.Single().Value.ShouldEqual("IdempotencyKeyUnverifiable: Idempotency key 'one' has an unreadable stored fingerprint, so this retry cannot be compared. Nothing was written.");
    [Fact] async Task should_not_write() => await ShouldNotWrite();
}
