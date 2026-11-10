// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultKeyStoreMigrator;

public class when_running_a_dry_run : given.a_migrator
{
    EncryptionKeyMigrationReport _report;

    void Establish() => _vault.Put(Legacy(1), Key(1));

    async Task Because() => _report = await Migrate(dryRun: true);

    [Fact] void should_not_write_anything() => _vault.Has(Current(1)).ShouldBeFalse();
    [Fact] void should_not_take_the_lock() => _vault.Has(MigrationOwnership.LockName).ShouldBeFalse();
    [Fact] void should_report_what_would_be_copied() => _report.Copied.ShouldEqual(1L);
}
