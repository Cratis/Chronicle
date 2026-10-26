// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.InMemory;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Events.Constraints.StoreKeys.for_EventStoreStorage;

public class when_accessing_keys_for_different_namespaces_and_stores : Specification
{
    IEventStoreStorage _first;
    IEventStoreStorage _second;
    UniqueConstraintEncodingKey _key;
    UniqueConstraintEncodingKey? _otherStoreKey;
    UniqueConstraintEncodingKey _afterNamespaceAccess;
    UniqueConstraintEncodingKey _otherCandidate;
    UniqueConstraintEncodingKey _otherStoreCreated;

    void Establish()
    {
        var jobs = Substitute.For<Cratis.Orleans.Storage.IJobsStorage>();
        _first = new EventStoreStorage("first", _ => Substitute.For<ISinks>(), jobs);
        _second = new EventStoreStorage("second", _ => Substitute.For<ISinks>(), jobs);
        _key = UniqueConstraintEncodingKey.Create();
        _otherCandidate = UniqueConstraintEncodingKey.Create();
    }

    async Task Because()
    {
        _first.GetNamespace("one");
        await _first.UniqueConstraintEncoding.CreateKeyIfAbsent(_key);
        _first.GetNamespace("two");
        _afterNamespaceAccess = (await _first.UniqueConstraintEncoding.GetKey())!;
        _otherStoreKey = await _second.UniqueConstraintEncoding.GetKey();
        _otherStoreCreated = await _second.UniqueConstraintEncoding.CreateKeyIfAbsent(_otherCandidate);
    }

    [Fact] void should_keep_one_key_across_namespace_access() => _afterNamespaceAccess.ShouldEqual(_key);
    [Fact] void should_not_leak_a_key_to_another_store() => _otherStoreKey.ShouldBeNull();
    [Fact] void should_allow_another_store_to_provision_its_own_key() => _otherStoreCreated.ShouldEqual(_otherCandidate);
    [Fact] void should_reuse_the_store_level_accessor() => ReferenceEquals(_first.UniqueConstraintEncoding, _first.UniqueConstraintEncoding).ShouldBeTrue();
}
