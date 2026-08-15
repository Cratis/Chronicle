// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_getting_a_key;

/// <summary>
/// A composite reads through its stores in order and heals the ones that were missing a key it found later. That is
/// a second, independent resurrection path: erase the primary and the next read copies the secondary's survivor
/// straight back into it.
/// </summary>
/// <remarks>
/// The fence tells the composite this absence is an erasure rather than a gap to fill. It stops the healing, and it
/// stops the key being served at all - handing back a copy of a key that was erased is the same personal data the
/// erasure was supposed to make unreadable, wherever it is read from. Reaching this state means the erasure did not
/// reach every store, which it reported at the time.
/// </remarks>
public class and_the_primary_store_erased_it_while_the_secondary_still_holds_it : given.two_key_stores
{
    EncryptionKey? _result;
    EncryptionKey? _inPrimaryAfterwards;

    async Task Establish()
    {
        var survivor = KeyNamed("survivor");
        await Save(_primary, survivor);
        await Save(_secondary, survivor);

        // The erasure reached the primary and not the secondary - a store that was unreachable at the time, or a
        // silo that had not caught up. The fence is durable in the primary either way.
        await _primary.RecordErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        await _primary.DeleteFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
    }

    async Task Because()
    {
        _result = await KeyIn(_composite);
        _inPrimaryAfterwards = await KeyIn(_primary);
    }

    [Fact] void should_not_serve_the_surviving_key() => _result.ShouldBeNull();
    [Fact] void should_not_heal_it_back_into_the_erased_store() => _inPrimaryAfterwards.ShouldBeNull();
}
