// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_idempotency_key;

public class and_the_subject_was_erased_with_the_same_non_personal_content : given.an_event_sequence_with_idempotency_keys
{
    async Task Establish() => await Protect(erased: true);
    async Task Because() => _result = await AppendKeyed();
    [Fact] void should_settle_after_erasure() => _result.Outcome.ShouldEqual(AppendOutcome.AlreadyAppendedAfterErasure);
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_not_provision_a_key() => await _keys.DidNotReceive().GetOrAddFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<EncryptionKeyIdentifier>(), Arg.Any<EncryptionKey>());
    [Fact] async Task should_not_write() => await ShouldNotWrite();
}
