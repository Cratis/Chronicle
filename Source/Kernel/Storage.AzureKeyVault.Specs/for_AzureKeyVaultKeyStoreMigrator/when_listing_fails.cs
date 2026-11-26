// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultKeyStoreMigrator;

public class when_listing_fails : given.a_migrator
{
    EncryptionKeyMigrationReport _report;

    void Establish()
    {
        _vault.Put(Legacy(1), Key(1));
        _vault.FailListing = true;
    }

    async Task Because() => _report = await Migrate();

    [Fact] void should_report_a_failure() => _report.Failed.ShouldEqual(1L);
    [Fact] void should_not_be_complete() => _report.Complete.ShouldBeFalse();
    [Fact] void should_release_the_lock() => _vault.Latest(MigrationOwnership.LockName)!.StartsWith("released:", StringComparison.Ordinal).ShouldBeTrue();
}
