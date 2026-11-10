// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreMigrator;

public class when_a_legacy_path_is_ambiguous : given.a_migrator
{
    EncryptionKeyMigrationReport _report;

    void Establish()
    {
        _store.Put("store/default/a/b/1", Key(1));
        _store.Put($"{Address}/01", Key(2));
    }

    async Task Because() => _report = await Migrate(remove: true);

    [Fact] void should_count_them_for_review() => _report.AmbiguousForReview.ShouldEqual(2L);
    [Fact] void should_need_review() => _report.NeedsReview.ShouldBeTrue();
    [Fact] void should_not_write_under_the_current_names() => _store.Paths.Any(path => path.StartsWith("k1/", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_keep_the_first() => _store.Has("store/default/a/b/1").ShouldBeTrue();
    [Fact] void should_keep_the_second() => _store.Has($"{Address}/01").ShouldBeTrue();
}
