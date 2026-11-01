// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_InMemoryEncryptionKeyStorage.when_saving_a_key;

/// <summary>
/// Saving a later revision must not displace the earlier one. Rotation and idempotent-append comparison read earlier
/// revisions explicitly, so the earlier key has to stay readable at its own revision.
/// </summary>
public class and_a_later_revision_is_saved : given.an_in_memory_key_store
{
    EncryptionKey _earlier;
    EncryptionKey _later;
    EncryptionKey? _atEarlier;
    EncryptionKeyAtRevision? _latest;
    int _count;

    async Task Establish()
    {
        _earlier = KeyNamed("earlier");
        _later = KeyNamed("later");
        await Save(_earlier, EncryptionKeyRevision.Initial);
    }

    async Task Because()
    {
        await Save(_later);
        _atEarlier = await _store.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, EncryptionKeyRevision.Initial);
        _latest = await _store.TryGetLatestFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        _count = await RevisionCount();
    }

    [Fact] void should_still_return_the_earlier_key_at_its_revision() => _atEarlier.ShouldEqual(_earlier);
    [Fact] void should_report_the_later_key_as_latest() => _latest.Key.ShouldEqual(_later);
    [Fact] void should_put_the_later_key_at_the_next_revision() => _latest.Revision.ShouldEqual(new EncryptionKeyRevision(2u));
    [Fact] void should_keep_both_revisions() => _count.ShouldEqual(2);
}
