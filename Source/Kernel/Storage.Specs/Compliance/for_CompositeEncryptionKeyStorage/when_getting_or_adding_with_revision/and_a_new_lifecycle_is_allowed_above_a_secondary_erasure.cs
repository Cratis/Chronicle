// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_getting_or_adding_with_revision;

public class and_a_new_lifecycle_is_allowed_above_a_secondary_erasure : given.two_key_stores
{
    Exception _error;
    EncryptionKeyAtRevision? _result;
    EncryptionKeyAtRevision? _primaryPair;
    EncryptionKeyAtRevision? _secondaryPair;
    EncryptionKey _legacyKey;
    EncryptionKey _candidate;

    async Task Establish()
    {
        _candidate = KeyNamed("candidate");
        await Save(_secondary, KeyNamed("erased"), new EncryptionKeyRevision(7u));
        await _secondary.RecordErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        await _secondary.DeleteFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        await _secondary.AllowNewKeyFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
    }

    async Task Because()
    {
        _error = await Catch.Exception(async () => _result = await _composite.GetOrAddWithRevisionFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _candidate));
        _primaryPair = await _primary.TryGetLatestFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        _secondaryPair = await _secondary.TryGetLatestFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        _legacyKey = await _composite.GetOrAddFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _candidate);
    }

    [Fact] void should_not_refuse_after_saving_on_the_primary() => _error.ShouldBeNull();
    [Fact] void should_report_the_primary_pair() => _result.ShouldEqual(_primaryPair);
    [Fact] void should_return_the_primary_revision() => _result!.Revision.ShouldEqual(EncryptionKeyRevision.Initial);
    [Fact] void should_mirror_the_key_above_the_secondary_erasure_floor() => _secondaryPair!.Revision.ShouldEqual(new EncryptionKeyRevision(8u));
    [Fact] void should_mirror_the_primary_key() => _secondaryPair!.Key.Public.ShouldEqual(_candidate.Public);
    [Fact] void should_match_key_only_provisioning() => _result!.Key.ShouldEqual(_legacyKey);
}
