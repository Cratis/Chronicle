// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreMigrator;

public class when_removing_verified_legacy : given.a_migrator
{
    EncryptionKeyMigrationReport _report;

    void Establish()
    {
        _store.Put($"{Address}/erasure", new Dictionary<string, string> { ["erasedThrough"] = "0" });
        _store.Put($"{Address}/1", Key(1));
        _store.Put("store/default/a/b/1", Key(2));
    }

    async Task Because() => _report = await Migrate(remove: true);

    [Fact] void should_remove_the_verified_key() => _store.Has($"{Address}/1").ShouldBeFalse();
    [Fact] void should_remove_the_verified_erasure_record() => _store.Has($"{Address}/erasure").ShouldBeFalse();
    [Fact] void should_keep_the_copies() => _store.Has($"{Current}/1").ShouldBeTrue();
    [Fact] void should_keep_the_copied_erasure_record() => _store.Has($"{Current}/erasure").ShouldBeTrue();
    [Fact] void should_never_touch_what_cannot_be_addressed() => _store.Has("store/default/a/b/1").ShouldBeTrue();
    [Fact] void should_count_what_it_removed() => _report.LegacyRemoved.ShouldEqual(2L);
}
