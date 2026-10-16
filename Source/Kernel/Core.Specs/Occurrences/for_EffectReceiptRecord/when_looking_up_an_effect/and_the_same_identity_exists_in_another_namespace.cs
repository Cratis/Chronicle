// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences.for_EffectReceiptRecord.when_looking_up_an_effect;

public class and_the_same_identity_exists_in_another_namespace : Specification
{
    IStorage _storage;
    EffectReceiptRecord? _first;
    EffectReceiptRecord? _second;
    EffectReceiptRecord? _absent;

    async Task Establish()
    {
        _storage = Substitute.For<IStorage>();
        var store = Substitute.For<IEventStoreStorage>();
        _storage.GetEventStore((EventStoreName)"store").Returns(store);
        foreach (var name in new[] { "first", "second" })
        {
            var ns = Substitute.For<IEventStoreNamespaceStorage>();
            var sequence = new Storage.InMemory.EventSequences.EventSequenceStorage("store", name, EventSequenceId.Log, Substitute.For<IIdentityStorage>());
            store.GetNamespace((EventStoreNamespaceName)name).Returns(ns);
            ns.GetEventSequence(EventSequenceId.Log).Returns(sequence);
            var receipt = new Storage.Occurrences.EffectReceipt(OccurrenceIdentities.ForEffect(new("occurrence"), "command"), new(new("occurrence"), 3), EffectOutcome.NoEvents, null, null, null, [], $"\"{name}\"", DateTimeOffset.Parse("2026-10-14T12:00:00Z"));
            await sequence.AppendManyWithEffectReceipt([], receipt);
        }
    }

    async Task Because()
    {
        _first = await EffectReceiptRecord.GetEffectReceipt("store", "first", "occurrence", "command", _storage);
        _second = await EffectReceiptRecord.GetEffectReceipt("store", "second", "occurrence", "command", _storage);
        _absent = await EffectReceiptRecord.GetEffectReceipt("store", "second", "occurrence", "other-command", _storage);
    }

    [Fact] void should_find_the_first_namespaces_response() => _first!.Response.ShouldEqual("\"first\"");
    [Fact] void should_find_the_second_namespaces_response() => _second!.Response.ShouldEqual("\"second\"");
    [Fact] void should_not_find_another_effect() => _absent.ShouldBeNull();
}
