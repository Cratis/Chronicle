// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultKeyStoreMigrator;

public class when_the_catalog_knows_more_scopes : given.a_migrator
{
    EncryptionKeyMigrationReport _report;

    void Establish()
    {
        _catalog = Substitute.For<IEncryptionKeyScopeCatalog>();
        _catalog.GetScopes().Returns([new LegacyScope(Store, Namespace), new LegacyScope("Other", "Scope")]);
        _vault.Put(Legacy(1), Key(1));
        _vault.Put("chronicle--other--scope--customer-42--1", Key(2));
    }

    async Task Because() => _report = await Migrate();

    [Fact] void should_report_the_missing_scope() => _report.ScopesMissingFromList.ShouldEqual(1L);
    [Fact] void should_not_add_the_scope() => _vault.Has(SecretLocations.For("Other", "Scope", _identifier).Name(1)).ShouldBeFalse();
    [Fact] void should_not_fail() => _report.Failed.ShouldEqual(0L);
}
