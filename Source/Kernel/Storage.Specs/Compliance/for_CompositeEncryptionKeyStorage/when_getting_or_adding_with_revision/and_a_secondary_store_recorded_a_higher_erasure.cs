// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_CompositeEncryptionKeyStorage.when_getting_or_adding_with_revision;

public class and_a_secondary_store_recorded_a_higher_erasure : given.two_key_stores
{
    Exception _error;
    Exception _legacyError;
    EncryptionKey? _primaryKey;

    async Task Establish()
    {
        await Save(_secondary, KeyNamed("erased"), new EncryptionKeyRevision(7u));
        await _secondary.RecordErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        await _secondary.DeleteFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
    }

    async Task Because()
    {
        _legacyError = await Catch.Exception(() => _composite.GetOrAddFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, KeyNamed("candidate")));
        _error = await Catch.Exception(() => _composite.GetOrAddWithRevisionFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, KeyNamed("candidate")));
        _primaryKey = await KeyIn(_primary);
    }

    [Fact] void should_refuse_to_provision() => _error.ShouldBeOfExactType<EncryptionKeyErased>();
    [Fact] void should_match_key_only_provisioning() => _legacyError.ShouldBeOfExactType<EncryptionKeyErased>();
    [Fact] void should_leave_the_primary_without_a_key() => _primaryKey.ShouldBeNull();
}
