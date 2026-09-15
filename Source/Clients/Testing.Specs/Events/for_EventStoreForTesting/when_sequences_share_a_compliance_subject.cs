// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

namespace Cratis.Chronicle.Testing.Events.for_EventStoreForTesting;

public class when_sequences_share_a_compliance_subject : Specification
{
    EventStoreForTesting _store;
    EventStoreForTesting _otherStore;
    EventSourceId _id;
    object _firstRead;
    object _secondRead;
    object _isolatedRead;
    IDictionary _keys;
    IDictionary _otherKeys;

    void Establish()
    {
        _store = new EventStoreForTesting();
        _otherStore = new EventStoreForTesting();
        _id = EventSourceId.New();
    }

    async Task Because()
    {
        var secondary = _store.GetEventSequence(new EventSequenceId("secondary"));
        (await _store.EventLog.Append(_id, new MemberEnrolledWithEmail("first@example.com"))).ShouldBeSuccessful();
        (await secondary.Append(_id, new MemberEnrolledWithEmail("second@example.com"))).ShouldBeSuccessful();
        (await _otherStore.EventLog.Append(_id, new MemberEnrolledWithEmail("isolated@example.com"))).ShouldBeSuccessful();
        _firstRead = (await _store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, _id))[0].Content;
        _secondRead = (await secondary.GetFromSequenceNumber(EventSequenceNumber.First, _id))[0].Content;
        _isolatedRead = (await _otherStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, _id))[0].Content;
        _keys = KeysIn(_store);
        _otherKeys = KeysIn(_otherStore);
    }

    [Fact] void should_release_the_first_sequences_value() => _firstRead.ShouldEqual(new MemberEnrolledWithEmail("first@example.com"));
    [Fact] void should_release_the_second_sequences_value() => _secondRead.ShouldEqual(new MemberEnrolledWithEmail("second@example.com"));
    [Fact] void should_release_the_isolated_scenarios_value() => _isolatedRead.ShouldEqual(new MemberEnrolledWithEmail("isolated@example.com"));
    [Fact] void should_mint_one_subject_key_across_sequences() => _keys.Count.ShouldEqual(1);
    [Fact] void should_mint_an_independent_key_in_another_scenario() => _otherKeys.Count.ShouldEqual(1);
    [Fact] void should_not_share_subject_keys_between_scenarios() => ReferenceEquals(_keys.Values.Cast<object>().Single(), _otherKeys.Values.Cast<object>().Single()).ShouldBeFalse();

    static IDictionary KeysIn(EventStoreForTesting store)
    {
        // Kernel assemblies are private package implementation details. Inspect only in harness specs,
        // without adding a consumer-facing API for raw content or encryption keys.
        var compliance = typeof(EventStoreForTesting).GetField("_compliance", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(store)!;
        var storage = compliance.GetType().GetProperty("KeyStorage")!.GetValue(compliance)!;
        return (IDictionary)storage.GetType().GetField("_keys", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(storage)!;
    }
}
