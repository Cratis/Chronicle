// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.InMemory.Compliance.for_EncryptionKeyStorage.when_saving_a_key;

/// <summary>
/// Saving a later revision must not displace the earlier one. Rotation and idempotent-append comparison read earlier
/// revisions explicitly, so the earlier key has to stay readable at its own revision.
/// </summary>
public class and_a_later_revision_is_saved : Specification
{
    IEncryptionKeyStorage _store;
    EncryptionKeyIdentifier _identifier = Guid.NewGuid().ToString();
    EncryptionKey _earlier = new([1], [2]);
    EncryptionKey _later = new([3], [4]);
    EncryptionKey? _atEarlier;
    EncryptionKeyAtRevision? _latest;
    bool _hasEarlier;

    async Task Establish()
    {
        _store = new EncryptionKeyStorage();
        await _store.SaveFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _earlier, EncryptionKeyRevision.Initial);
    }

    async Task Because()
    {
        await _store.SaveFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _later);
        _atEarlier = await _store.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, EncryptionKeyRevision.Initial);
        _latest = await _store.TryGetLatestFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        _hasEarlier = await _store.HasFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, EncryptionKeyRevision.Initial);
    }

    [Fact] void should_still_return_the_earlier_public_key_at_its_revision() => _atEarlier.Public.ShouldEqual(_earlier.Public);
    [Fact] void should_still_return_the_earlier_private_key_at_its_revision() => _atEarlier.Private.ShouldEqual(_earlier.Private);
    [Fact] void should_report_the_later_public_key_as_latest() => _latest.Key.Public.ShouldEqual(_later.Public);
    [Fact] void should_report_the_later_private_key_as_latest() => _latest.Key.Private.ShouldEqual(_later.Private);
    [Fact] void should_put_the_later_key_at_the_next_revision() => _latest.Revision.ShouldEqual(new EncryptionKeyRevision(2u));
    [Fact] void should_keep_the_earlier_revision() => _hasEarlier.ShouldBeTrue();
}
