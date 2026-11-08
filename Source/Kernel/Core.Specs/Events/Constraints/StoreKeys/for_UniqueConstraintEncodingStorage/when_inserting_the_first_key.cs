// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.StoreKeys.for_UniqueConstraintEncodingStorage;

public class when_inserting_the_first_key : given.a_key_storage
{
    UniqueConstraintEncodingKey _candidate;
    UniqueConstraintEncodingKey _created;
    UniqueConstraintEncodingKey _first;
    UniqueConstraintEncodingKey _second;

    void Establish() => _candidate = UniqueConstraintEncodingKey.Create();

    async Task Because()
    {
        _created = await _storage.CreateKeyIfAbsent(_candidate);
        _first = (await _storage.GetKey())!;
        _second = (await _storage.GetKey())!;
    }

    [Fact] void should_return_the_supplied_pair() => _created.ShouldEqual(_candidate);
    [Fact] void should_preserve_the_supplied_identity() => _first.KeyId.ShouldEqual(_candidate.KeyId);
    [Fact] void should_preserve_the_supplied_material() => Assert.Equal(_candidate.Material.CopyBytes(), _first.Material.CopyBytes());
    [Fact] void should_return_the_same_pair_on_repeated_reads() => _second.ShouldEqual(_first);
}
