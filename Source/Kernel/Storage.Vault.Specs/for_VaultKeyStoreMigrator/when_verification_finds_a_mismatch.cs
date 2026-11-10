// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreMigrator;

public class when_verification_finds_a_mismatch : given.a_migrator
{
    EncryptionKeyMigrationReport _report;

    void Establish()
    {
        _store.Put($"{Address}/1", Key(1));
        _store.Put($"{Address}/2", Key(2));
        _store.AfterWrite = path =>
        {
            if (path == $"{Current}/1")
            {
                _store.Put(path, WithLayout(Key(8)));
            }
        };
    }

    async Task Because() => _report = await Migrate(remove: true);

    [Fact] void should_count_the_failure() => _report.Failed.ShouldEqual(1L);
    [Fact] void should_verify_only_the_matching_copy() => _report.Verified.ShouldEqual(1L);
    [Fact] void should_not_remove_anything() => _report.LegacyRemoved.ShouldEqual(0L);
    [Fact] void should_keep_the_keys_under_earlier_names() => _store.Has($"{Address}/2").ShouldBeTrue();
}
