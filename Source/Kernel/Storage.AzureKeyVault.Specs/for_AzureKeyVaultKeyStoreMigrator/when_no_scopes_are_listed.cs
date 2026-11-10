// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultKeyStoreMigrator;

public class when_no_scopes_are_listed : given.a_migrator
{
    EncryptionKeyMigrationReport _report;

    void Establish()
    {
        _scopes = [];
        _vault.Put(Legacy(1), Key(1));
    }

    async Task Because() => _report = await Migrate();

    [Fact] void should_fail() => _report.Failed.ShouldEqual(1L);
    [Fact] void should_not_be_complete() => _report.Complete.ShouldBeFalse();
    [Fact] void should_copy_nothing() => _vault.Has(Current(1)).ShouldBeFalse();
}
