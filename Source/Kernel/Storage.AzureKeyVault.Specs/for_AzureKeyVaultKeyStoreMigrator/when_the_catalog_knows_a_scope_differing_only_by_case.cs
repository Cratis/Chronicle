// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultKeyStoreMigrator;

public class when_the_catalog_knows_a_scope_differing_only_by_case : given.a_migrator
{
    EncryptionKeyMigrationReport _report;

    void Establish()
    {
        _catalog = Substitute.For<IEncryptionKeyScopeCatalog>();
        _catalog.GetScopes().Returns([new LegacyScope(Store, Namespace), new LegacyScope(Store.ToUpperInvariant(), Namespace)]);
        _vault.Put(Legacy(1), Key(1));
    }

    async Task Because() => _report = await Migrate(remove: true);

    [Fact] void should_leave_the_name_for_review() => _report.AmbiguousForReview.ShouldEqual(1L);
    [Fact] void should_not_copy_it() => _vault.Has(Current(1)).ShouldBeFalse();
    [Fact] void should_not_remove_the_earlier_secret() => _vault.Has(Legacy(1)).ShouldBeTrue();
}
