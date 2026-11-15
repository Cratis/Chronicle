// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreMigrator;

public class when_another_migrator_tries_to_remove_the_legacy_after_the_copy : given.a_migrator
{
    EncryptionKeyMigrationReport _first;
    EncryptionKeyMigrationReport _second;

    void Establish()
    {
        _store.Put($"{Address}/1", Key(1));
        _store.AfterWrite = path =>
        {
            if (path == $"{Current}/1")
            {
                // A has created the destination but has not checked its source again. B would verify the copy and
                // remove the source here; A would then roll back the only remaining copy. B must refuse instead.
                var other = new VaultKeyStoreMigrator(_store, NullLogger<VaultKeyStoreMigrator>.Instance);
                _second = other.Migrate(false, true).GetAwaiter().GetResult();
            }
        };
    }

    async Task Because() => _first = await Migrate();

    [Fact] void should_refuse_the_second_migrator() => _second.MigrationLocked.ShouldBeTrue();
    [Fact] void should_not_let_the_second_migrator_remove_the_source() => _store.Has($"{Address}/1").ShouldBeTrue();
    [Fact] void should_keep_the_destination() => _store.Has($"{Current}/1").ShouldBeTrue();
    [Fact] void should_complete_the_owner() => _first.Complete.ShouldBeTrue();
    [Fact] void should_release_ownership() => _store.Has(VaultKeyStoreMigrator.MigrationLockPath).ShouldBeFalse();
}
