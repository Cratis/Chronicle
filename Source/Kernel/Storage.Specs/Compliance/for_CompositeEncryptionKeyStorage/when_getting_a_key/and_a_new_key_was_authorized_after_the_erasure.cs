// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_getting_a_key;

/// <summary>
/// The fence outlives the erasure, so a composite that refused every key for an identifier it had ever erased would
/// turn the erasure into the permanent ban the ruling says it is not.
/// </summary>
/// <remarks>
/// What the read refuses is destroyed key <i>material</i>, not the identifier - so the successor a later lifecycle
/// provisions is served normally, and only the bytes an erasure destroyed stay refused.
/// </remarks>
public class and_a_new_key_was_authorized_after_the_erasure : given.two_key_stores
{
    EncryptionKey _successor;
    EncryptionKey? _result;

    async Task Establish()
    {
        _successor = KeyNamed("successor");
        await Save(_primary, KeyNamed("original"));
        await Save(_secondary, KeyNamed("original"));

        await _composite.RecordErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        await _composite.DeleteFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        await _composite.AllowNewKeyFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        await _composite.GetOrAddFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _successor);
    }

    async Task Because() => _result = await KeyIn(_composite);

    [Fact] void should_serve_the_successor_key() => _result.ShouldEqual(_successor);
}
