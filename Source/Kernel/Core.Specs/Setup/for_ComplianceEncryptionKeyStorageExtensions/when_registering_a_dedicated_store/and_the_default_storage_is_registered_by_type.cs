// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Setup.for_ComplianceEncryptionKeyStorageExtensions.when_registering_a_dedicated_store;

public class and_the_default_storage_is_registered_by_type : given.a_chronicle_builder
{
    IEncryptionKeyStorage _resolved;
    EncryptionKey _provisioned;
    EncryptionKey? _servedAfterTheDedicatedStorageLostIt;

    void Establish()
    {
        // The in-memory backend registers its key storage by implementation type rather than through a factory.
        _services.AddSingleton<IEncryptionKeyStorage, InMemoryEncryptionKeyStorage>();
    }

    async Task Because()
    {
        RegisterTheDedicatedStorage(migrate: true);
        _resolved = Resolve();
        _provisioned = await _resolved.GetOrAddFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, _keyOnlyInTheDefaultStorage);

        // Only the mirrored copy in the composed default storage can answer this.
        await _dedicatedStorage.DeleteFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
        _servedAfterTheDedicatedStorageLostIt = await KeyIn(_resolved);
    }

    [Fact] void should_compose_the_two_stores() => _resolved.ShouldBeOfExactType<CompositeEncryptionKeyStorage>();
    [Fact] void should_provision_on_the_dedicated_storage() => _provisioned.ShouldEqual(_keyOnlyInTheDefaultStorage);
    [Fact] void should_build_the_default_storage_from_its_implementation_type_and_mirror_to_it() => _servedAfterTheDedicatedStorageLostIt.ShouldEqual(_keyOnlyInTheDefaultStorage);
}
