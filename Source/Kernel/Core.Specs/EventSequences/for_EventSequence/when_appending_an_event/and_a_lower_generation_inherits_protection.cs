// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

public class and_a_lower_generation_inherits_protection : given.an_event_sequence_with_inherited_protection
{
    AppendResult _result;

    async Task Because() => _result = await _eventSequence.Append(
        EventSourceType.Default,
        _eventSourceId,
        EventStreamType.All,
        EventStreamId.Default,
        _eventType,
        Plaintext("single-inherited-pii-marker"),
        CorrelationId.New(),
        [],
        Identity.System,
        [],
        ConcurrencyScope.None,
        subject: new Subject(SubjectIdentifier));

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_protect_every_stored_generation_exactly_once() => await AssertProtectedOnce();
    [Fact] void should_hash_the_stored_ciphertext() => AssertHashes();
    [Fact] async Task should_make_every_generation_unrecoverable_after_erasure() => await AssertErasure();
    [Fact] void should_leave_the_callers_plaintext_unchanged() => _plaintextInputs.Single()["name"]!.GetValue<string>().ShouldEqual("single-inherited-pii-marker");
}
