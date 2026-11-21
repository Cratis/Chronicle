// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultKeyStoreMigrator;

public class when_removing_verified_earlier_secrets : given.a_migrator
{
    EncryptionKeyMigrationReport _report;

    void Establish()
    {
        _vault.Put(Legacy(1), Key(1));
        _vault.Put(Legacy(2), Key(2));
        _vault.PurgeRefused = true;
    }

    async Task Because() => _report = await Migrate(remove: true);

    [Fact] void should_remove_the_earlier_secrets() => _vault.Has(Legacy(1)).ShouldBeFalse();
    [Fact] void should_keep_the_copies() => _vault.Latest(Current(2)).ShouldEqual(Key(2));
    [Fact] void should_report_that_the_vault_kept_the_deleted_copies() => _report.LegacyRemovedNotPurged.ShouldEqual(2L);
    [Fact] void should_not_count_them_as_purged() => _report.LegacyRemoved.ShouldEqual(0L);
    [Fact] void should_need_review() => _report.NeedsReview.ShouldBeTrue();
}
