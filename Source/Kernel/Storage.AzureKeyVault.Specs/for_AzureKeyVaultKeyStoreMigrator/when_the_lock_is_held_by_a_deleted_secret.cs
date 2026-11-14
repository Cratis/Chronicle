// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultKeyStoreMigrator;

public class when_the_lock_is_held_by_a_deleted_secret : given.a_migrator
{
    EncryptionKeyMigrationReport _report;

    void Establish()
    {
        _vault.Put(Legacy(1), Key(1));
        _vault.MarkDeleted(MigrationOwnership.LockName);
    }

    async Task Because() => _report = await Migrate(remove: true);

    [Fact] void should_not_own_the_lock() => _report.MigrationLocked.ShouldBeTrue();
    [Fact] void should_copy_nothing() => _vault.Has(Current(1)).ShouldBeFalse();
    [Fact] void should_remove_nothing() => _vault.Has(Legacy(1)).ShouldBeTrue();
}
