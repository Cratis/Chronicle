// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many;

[PerFactSpecificationLifecycle]
public class and_a_lower_generation_inherits_protection : given.an_event_sequence_with_inherited_protection
{
    AppendManyResult _result;

    async Task Because() => _result = await _eventSequence.AppendMany(
        [EventFor("first-inherited-pii-marker"), EventFor("second-inherited-pii-marker")],
        CorrelationId.New(),
        [],
        Identity.System,
        new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_store_both_events() => _storedEvents.Count.ShouldEqual(2);
    [Fact] async Task should_protect_every_stored_generation_exactly_once() => await AssertProtectedOnce();
    [Fact] void should_hash_the_stored_ciphertext() => AssertHashes();
    [Fact] async Task should_make_every_generation_unrecoverable_after_erasure() => await AssertErasure();
    [Fact] void should_leave_the_first_plaintext_unchanged() => _plaintextInputs[0]["name"]!.GetValue<string>().ShouldEqual("first-inherited-pii-marker");
    [Fact] void should_leave_the_second_plaintext_unchanged() => _plaintextInputs[1]["name"]!.GetValue<string>().ShouldEqual("second-inherited-pii-marker");

    EventToAppend EventFor(string marker) => new(
        EventSourceType.Default,
        _eventSourceId,
        EventStreamType.All,
        EventStreamId.Default,
        _eventType,
        [],
        Plaintext(marker),
        Subject: new Subject(SubjectIdentifier));
}
