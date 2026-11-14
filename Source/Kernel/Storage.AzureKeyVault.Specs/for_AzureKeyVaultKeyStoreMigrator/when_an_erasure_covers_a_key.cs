// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultKeyStoreMigrator;

public class when_an_erasure_covers_a_key : given.a_migrator
{
    EncryptionKeyMigrationReport _report;

    void Establish()
    {
        _vault.Put(LegacyErasure, Erasure(1, FingerprintOf(3)));
        _vault.Put(Legacy(1), Key(1));
        _vault.Put(Legacy(2), Key(2));
        _vault.Put(Legacy(3), Key(3));
    }

    async Task Because() => _report = await Migrate();

    [Fact] void should_copy_the_erasure_record() => _vault.Latest(CurrentErasure).ShouldEqual(Erasure(1, FingerprintOf(3)));
    [Fact] void should_not_copy_a_key_below_the_floor() => _vault.Has(Current(1)).ShouldBeFalse();
    [Fact] void should_not_copy_an_erased_fingerprint() => _vault.Has(Current(3)).ShouldBeFalse();
    [Fact] void should_copy_a_key_the_erasure_does_not_cover() => _vault.Has(Current(2)).ShouldBeTrue();
    [Fact] void should_count_the_erasure_record() => _report.ErasureRecordsCopied.ShouldEqual(1L);
    [Fact] void should_count_the_withheld_keys_for_review() => _report.ConflictsKept.ShouldEqual(2L);
}
