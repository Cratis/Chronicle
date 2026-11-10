// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultKeyStoreMigrator;

public class when_migrating_unambiguous_keys : given.a_migrator
{
    EncryptionKeyMigrationReport _report;

    void Establish()
    {
        _vault.Put(Legacy(1), Key(1));
        _vault.Put(Legacy(2), Key(2));
    }

    async Task Because() => _report = await Migrate();

    [Fact] void should_copy_the_first_revision() => _vault.Latest(Current(1)).ShouldEqual(Key(1));
    [Fact] void should_copy_the_second_revision() => _vault.Latest(Current(2)).ShouldEqual(Key(2));
    [Fact] void should_keep_the_earlier_names() => _vault.Has(Legacy(1)).ShouldBeTrue();
    [Fact] void should_count_the_copies() => _report.Copied.ShouldEqual(2L);
    [Fact] void should_verify_the_copies() => _report.Verified.ShouldEqual(2L);
    [Fact] void should_be_complete() => _report.Complete.ShouldBeTrue();
    [Fact] void should_not_need_review() => _report.NeedsReview.ShouldBeFalse();
    [Fact] void should_release_the_lock() => _vault.Latest(MigrationOwnership.LockName)!.StartsWith("released:", StringComparison.Ordinal).ShouldBeTrue();
}
