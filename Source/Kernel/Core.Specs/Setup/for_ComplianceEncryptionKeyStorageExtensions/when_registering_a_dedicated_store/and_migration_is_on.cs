// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Setup.for_ComplianceEncryptionKeyStorageExtensions.when_registering_a_dedicated_store;

public class and_migration_is_on : given.a_chronicle_builder
{
    IEncryptionKeyStorage _resolved;
    EncryptionKey? _keyThroughTheResolvedStorage;
    EncryptionKey? _healedIntoTheDedicatedStorage;
    int _registrations;

    async Task Establish()
    {
        // The MongoDB and SQL backends register their key storage through a factory, so that is the shape the
        // composition has to take over from.
        _services.AddSingleton<IEncryptionKeyStorage>(_ => _defaultStorage);
        await SeedTheDefaultStorage();
    }

    async Task Because()
    {
        RegisterTheDedicatedStorage(migrate: true);
        _registrations = RegistrationsOfEncryptionKeyStorage();
        _resolved = Resolve();
        _keyThroughTheResolvedStorage = await KeyIn(_resolved);
        _healedIntoTheDedicatedStorage = await KeyIn(_dedicatedStorage);
    }

    [Fact] void should_compose_the_two_stores() => _resolved.ShouldBeOfExactType<CompositeEncryptionKeyStorage>();
    [Fact] void should_keep_serving_a_key_that_only_exists_in_the_default_storage() => _keyThroughTheResolvedStorage.ShouldEqual(_keyOnlyInTheDefaultStorage);
    [Fact] void should_move_that_key_into_the_dedicated_storage() => _healedIntoTheDedicatedStorage.ShouldEqual(_keyOnlyInTheDefaultStorage);
    [Fact] void should_take_over_the_default_registration_rather_than_shadow_it() => _registrations.ShouldEqual(1);
    [Fact] void should_stay_reachable_by_the_cluster_wide_crypto_shred_eviction() => (_resolved is IEvictEncryptionKeyCache).ShouldBeTrue();
}
