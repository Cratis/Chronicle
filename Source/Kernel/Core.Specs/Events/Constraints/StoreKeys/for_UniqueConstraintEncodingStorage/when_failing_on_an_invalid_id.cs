// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.StoreKeys.for_UniqueConstraintEncodingStorage;

public class when_failing_on_an_invalid_id : given.a_key_storage
{
    string _materialHex;
    Exception _error;
    string _pairText;
    UniqueConstraintEncodingKey? _persisted;

    void Establish()
    {
        var material = new byte[32];
        Array.Fill(material, (byte)0xAB);
        _materialHex = Convert.ToHexString(material);
    }

    async Task Because()
    {
        var key = new UniqueConstraintEncodingKey(new UniqueConstraintStoreKeyId("not-canonical"), new UniqueConstraintStoreKey(Convert.FromHexString(_materialHex)));
        _pairText = key.ToString();
        _error = await Catch.Exception(async () => await _storage.CreateKeyIfAbsent(key));
        _persisted = await _storage.GetKey();
    }

    [Fact] void should_throw_invalid_key_id() => _error.ShouldBeOfExactType<InvalidUniqueConstraintStoreKeyId>();
    [Fact] void should_not_leak_material_in_the_message() => _error.Message.ToUpperInvariant().ShouldNotContain(_materialHex);
    [Fact] void should_not_leak_material_in_the_exception_text() => _error.ToString().ToUpperInvariant().ShouldNotContain(_materialHex);
    [Fact] void should_not_leak_material_in_the_pair_text() => _pairText.ToUpperInvariant().ShouldNotContain(_materialHex);
    [Fact] void should_not_persist_anything() => _persisted.ShouldBeNull();
}
