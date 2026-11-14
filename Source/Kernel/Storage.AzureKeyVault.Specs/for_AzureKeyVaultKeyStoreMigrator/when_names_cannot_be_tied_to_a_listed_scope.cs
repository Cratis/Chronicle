// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultKeyStoreMigrator;

public class when_names_cannot_be_tied_to_a_listed_scope : given.a_migrator
{
    EncryptionKeyMigrationReport _report;

    void Establish()
    {
        _vault.Put(Legacy(1), Key(1));
        _vault.Put("chronicle--unknown--scope--identifier--1", Key(2));
        _vault.Put("unrelated-secret", "value");
    }

    async Task Because() => _report = await Migrate(remove: true);

    [Fact] void should_leave_the_unresolved_name_alone() => _vault.Latest("chronicle--unknown--scope--identifier--1").ShouldEqual(Key(2));
    [Fact] void should_leave_unrelated_secrets_alone() => _vault.Latest("unrelated-secret").ShouldEqual("value");
    [Fact] void should_report_it_for_review() => _report.AmbiguousForReview.ShouldEqual(1L);
    [Fact] void should_still_copy_what_is_resolved() => _vault.Has(Current(1)).ShouldBeTrue();
}
