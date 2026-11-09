// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreMigrator;

public class when_an_erasure_record_exists : given.a_migrator
{
    readonly List<string> _written = [];
    EncryptionKeyMigrationReport _report;

    void Establish()
    {
        _store.Put($"{Address}/1", Key(1));
        _store.Put($"{Address}/2", Key(2));
        _store.Put($"{Address}/3", Key(3));
        _store.Put($"{Address}/erasure", new Dictionary<string, string>
        {
            ["erasedThrough"] = "1",
            ["erasedKeyFingerprints"] = FingerprintOf(3)
        });
        _store.AfterWrite = _written.Add;
    }

    async Task Because() => _report = await Migrate();

    [Fact] void should_copy_the_erasure_record_before_any_key() => _written[1].ShouldEqual($"{Current}/erasure");
    [Fact] void should_count_the_erasure_record() => _report.ErasureRecordsCopied.ShouldEqual(1L);
    [Fact] void should_not_copy_a_revision_it_erased() => _store.Has($"{Current}/1").ShouldBeFalse();
    [Fact] void should_not_copy_a_key_whose_fingerprint_it_holds() => _store.Has($"{Current}/3").ShouldBeFalse();
    [Fact] void should_copy_a_key_it_does_not_cover() => _store.Has($"{Current}/2").ShouldBeTrue();
    [Fact] void should_keep_the_erased_ones_for_review() => _report.ConflictsKept.ShouldEqual(2L);
}
