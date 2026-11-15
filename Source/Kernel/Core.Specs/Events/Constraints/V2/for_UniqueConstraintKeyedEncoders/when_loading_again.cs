// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintKeyedEncoders;

public class when_loading_again : given.a_loader
{
    static readonly EventStoreName _store = "store";
    UniqueConstraintKeyedEncoder? _first;
    UniqueConstraintKeyedEncoder? _second;
    IUniqueConstraintEncodingStorage _keyStorage;

    void Establish() => _keyStorage = KeyStorageFor(_store, UniqueConstraintEncodingKey.Create());

    async Task Because()
    {
        _first = await _loader.TryGetFor(_store);
        _second = await _loader.TryGetFor(_store);
    }

    [Fact] void should_read_the_key_once() => _keyStorage.Received(1).GetKey();
    [Fact] void should_return_the_cached_encoder() => ReferenceEquals(_second, _first).ShouldBeTrue();
}
