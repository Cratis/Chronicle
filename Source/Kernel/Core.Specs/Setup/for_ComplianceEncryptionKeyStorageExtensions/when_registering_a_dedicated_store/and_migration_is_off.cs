// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Setup.for_ComplianceEncryptionKeyStorageExtensions.when_registering_a_dedicated_store;

public class and_migration_is_off : given.a_chronicle_builder
{
    IEncryptionKeyStorage _resolved;
    EncryptionKey? _keyThroughTheResolvedStorage;

    async Task Establish()
    {
        _services.AddSingleton<IEncryptionKeyStorage>(_ => _defaultStorage);
        await SeedTheDefaultStorage();
    }

    async Task Because()
    {
        RegisterTheDedicatedStorage(migrate: false);
        _resolved = Resolve();
        _keyThroughTheResolvedStorage = await KeyIn(_resolved);
    }

    [Fact] void should_resolve_the_dedicated_storage() => _resolved.ShouldEqual(_dedicatedStorage);
    [Fact] void should_not_compose_it_with_the_default_storage() => _keyThroughTheResolvedStorage.ShouldBeNull();
}
