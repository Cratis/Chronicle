// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultKeyStoreMigrator;

public class when_a_listed_scope_has_no_source : given.a_migrator
{
    EncryptionKeyMigrationReport _report;

    void Establish()
    {
        _scopes.Add(new("Other", "Elsewhere"));
        _vault.Put(Legacy(1), Key(1));
    }

    async Task Because() => _report = await Migrate();

    [Fact] void should_fail() => _report.Failed.ShouldEqual(1L);
    [Fact] void should_not_be_complete() => _report.Complete.ShouldBeFalse();
    [Fact] void should_still_copy_the_listed_scope_that_has_a_source() => _vault.Latest(Current(1)).ShouldEqual(Key(1));
}
