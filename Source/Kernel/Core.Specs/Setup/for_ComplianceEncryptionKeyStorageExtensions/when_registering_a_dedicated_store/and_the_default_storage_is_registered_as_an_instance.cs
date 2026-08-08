// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Setup.for_ComplianceEncryptionKeyStorageExtensions.when_registering_a_dedicated_store;

public class and_the_default_storage_is_registered_as_an_instance : given.a_chronicle_builder
{
    IEncryptionKeyStorage _resolved;
    EncryptionKey? _keyThroughTheResolvedStorage;

    async Task Establish()
    {
        _services.AddSingleton<IEncryptionKeyStorage>(_defaultStorage);
        await SeedTheDefaultStorage();
    }

    async Task Because()
    {
        RegisterTheDedicatedStorage(migrate: true);
        _resolved = Resolve();
        _keyThroughTheResolvedStorage = await KeyIn(_resolved);
    }

    [Fact] void should_compose_the_two_stores() => _resolved.ShouldBeOfExactType<CompositeEncryptionKeyStorage>();
    [Fact] void should_keep_serving_a_key_that_only_exists_in_the_default_storage() => _keyThroughTheResolvedStorage.ShouldEqual(_keyOnlyInTheDefaultStorage);
}
