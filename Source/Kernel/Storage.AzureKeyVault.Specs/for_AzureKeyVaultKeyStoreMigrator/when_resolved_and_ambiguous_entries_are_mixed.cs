// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultKeyStoreMigrator;

public class when_resolved_and_ambiguous_entries_are_mixed : given.a_migrator
{
    EncryptionKeyMigrationReport _report;

    void Establish()
    {
        _vault.Put(Legacy(1), Key(1));
        _vault.Put("chronicle--unknown--scope--identifier--1", Key(2));
    }

    async Task Because() => _report = await Migrate(remove: true);

    [Fact] void should_copy_the_resolved_entry() => _vault.Latest(Current(1)).ShouldEqual(Key(1));
    [Fact] void should_keep_the_resolved_earlier_secret() => _vault.Has(Legacy(1)).ShouldBeTrue();
    [Fact] void should_keep_the_ambiguous_secret() => _vault.Has("chronicle--unknown--scope--identifier--1").ShouldBeTrue();
    [Fact] void should_remove_nothing() => _report.LegacyRemoved.ShouldEqual(0L);
    [Fact] void should_need_review() => _report.NeedsReview.ShouldBeTrue();
}
