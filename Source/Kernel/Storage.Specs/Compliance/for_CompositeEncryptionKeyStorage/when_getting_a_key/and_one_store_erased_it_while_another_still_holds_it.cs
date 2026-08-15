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
/// The fence in the primary is what tells the composite this absence is an erasure rather than a gap to fill. It
/// stops healing, and it also stops the key being served at all - handing back a copy of a key that was erased in
/// one member is the same personal data the erasure was supposed to make unreadable, wherever it is read from.
/// The state is reported as an incomplete erasure, because that is what it is.
/// </remarks>
public class and_one_store_erased_it_while_another_still_holds_it : given.two_key_stores
{
    EncryptionKey _survivor;
    EncryptionKey? _result;
    bool _hasIt;
    EncryptionKey? _inPrimaryAfterwards;

    async Task Establish()
    {
        _survivor = KeyNamed("survivor");
        await Save(_primary, _survivor);
        await Save(_secondary, _survivor);

        // The erasure reached the primary and not the secondary - a store that was unreachable at the time, or a
        // silo that had not caught up. The fence is durable in the primary either way.
        await _primary.RecordErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        await _primary.DeleteFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
    }

    async Task Because()
    {
        _result = await KeyIn(_composite);
        _hasIt = await HasKeyIn(_composite);
        _inPrimaryAfterwards = await KeyIn(_primary);
    }

    [Fact] void should_not_serve_the_surviving_key() => _result.ShouldBeNull();
    [Fact] void should_not_report_that_a_key_exists() => _hasIt.ShouldBeFalse();
    [Fact] void should_not_heal_it_back_into_the_erased_store() => _inPrimaryAfterwards.ShouldBeNull();
}
