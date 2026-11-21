// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultKeyStoreMigrator;

public class when_a_destination_already_differs : given.a_migrator
{
    EncryptionKeyMigrationReport _report;

    void Establish()
    {
        _vault.Put(Legacy(1), Key(1));
        _vault.Put(Current(1), Key(7));
        _vault.Put(Legacy(2), Key(2));
        _vault.Put(Current(2), Key(2));
        _vault.Put(Legacy(3), Key(3));
        _vault.MarkDeleted(Current(3));
    }

    async Task Because() => _report = await Migrate();

    [Fact] void should_not_overwrite_the_different_destination() => _vault.Latest(Current(1)).ShouldEqual(Key(7));
    [Fact] void should_not_write_over_a_recoverable_deleted_name() => _vault.Has(Current(3)).ShouldBeFalse();
    [Fact] void should_count_the_conflicts() => _report.ConflictsKept.ShouldEqual(2L);
    [Fact] void should_count_the_identical_one_as_migrated() => _report.AlreadyMigrated.ShouldEqual(1L);
    [Fact] void should_not_be_clean() => _report.NeedsReview.ShouldBeTrue();
}
