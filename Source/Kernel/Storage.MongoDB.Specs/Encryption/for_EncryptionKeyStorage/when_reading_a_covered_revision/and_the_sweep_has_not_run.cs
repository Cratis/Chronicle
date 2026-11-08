// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.MongoDB.for_ColdStartStorage.given;
using Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.given;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB.for_EncryptionKeyStorage.when_reading_a_covered_revision;

public class and_the_sweep_has_not_run : racing_encryption_key_collections
{
    EncryptionKeyAtRevision? _latest;
    EncryptionKey? _specific;
    bool _hasKey;

    void Establish()
    {
        _fence = new("identifier", 7u, [], false, 1, 1, 1);
        _keys.FindAsync(Arg.Any<FilterDefinition<EncryptionKeyForIdentifier>>(), Arg.Any<FindOptions<EncryptionKeyForIdentifier, EncryptionKeyForIdentifier>>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(duplicate_keys.CursorOf(new EncryptionKeyForIdentifier(new("identifier", 7u), [1], [2], true, 1, "owner"))));
    }

    async Task Because()
    {
        _latest = await _storage.TryGetLatestFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier");
        _specific = await _storage.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier", 7u);
        _hasKey = await _storage.HasFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "identifier");
    }

    [Fact] void should_hide_the_latest_covered_revision() => _latest.ShouldBeNull();
    [Fact] void should_hide_an_explicit_covered_revision() => _specific.ShouldBeNull();
    [Fact] void should_not_report_a_covered_key_as_present() => _hasKey.ShouldBeFalse();
}
