// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultKeyStoreMigrator;

public class when_an_entry_carries_attributes : given.a_migrator
{
    EncryptionKeyMigrationReport _report;

    void Establish()
    {
        _vault.Put(Legacy(1), Key(1), expires: DateTimeOffset.UnixEpoch.AddYears(80));
        _vault.Put(Legacy(2), Key(2), tags: new() { ["owner"] = "team" });
        _vault.Put(Legacy(3), Key(3), enabled: false);
        _vault.Put(Legacy(4), Key(4));
        _vault.Put(Legacy(4), Key(4));
        _vault.Put(Legacy(5), Key(5));
    }

    async Task Because() => _report = await Migrate(remove: true);

    [Fact] void should_not_copy_an_entry_with_an_expiry() => _vault.Has(Current(1)).ShouldBeFalse();
    [Fact] void should_not_copy_an_entry_with_tags() => _vault.Has(Current(2)).ShouldBeFalse();
    [Fact] void should_not_copy_a_disabled_entry() => _vault.Has(Current(3)).ShouldBeFalse();
    [Fact] void should_not_copy_an_entry_with_several_versions() => _vault.Has(Current(4)).ShouldBeFalse();
    [Fact] void should_copy_the_plain_entry() => _vault.Has(Current(5)).ShouldBeTrue();
    [Fact] void should_count_the_held_entries() => _report.HeldForAttributeDecision.ShouldEqual(4L);
    [Fact] void should_need_review() => _report.NeedsReview.ShouldBeTrue();
    [Fact] void should_not_remove_anything_while_entries_are_held() => _report.LegacyRemoved.ShouldEqual(0L);
    [Fact] void should_keep_the_plain_earlier_entry() => _vault.Has(Legacy(5)).ShouldBeTrue();
}
