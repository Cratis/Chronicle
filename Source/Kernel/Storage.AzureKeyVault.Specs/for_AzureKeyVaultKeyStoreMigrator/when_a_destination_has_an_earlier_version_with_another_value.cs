// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultKeyStoreMigrator;

public class when_a_destination_has_an_earlier_version_with_another_value : given.a_migrator
{
    EncryptionKeyMigrationReport _report;

    void Establish()
    {
        _vault.Put(Legacy(1), Key(1));
        _vault.Put(Current(1), Key(9));
        _vault.Put(Current(1), Key(1));
    }

    async Task Because() => _report = await Migrate(remove: true);

    [Fact] void should_keep_the_earlier_secret() => _vault.Has(Legacy(1)).ShouldBeTrue();
    [Fact] void should_not_remove_anything() => _report.LegacyRemoved.ShouldEqual(0L);
}
