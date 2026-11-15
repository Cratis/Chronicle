// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreMigrator;

public class when_destination_exists_with_different_content : given.a_migrator
{
    EncryptionKeyMigrationReport _report;

    void Establish()
    {
        _store.Put(VaultPathSegments.LayoutMarker, new Dictionary<string, string> { ["format"] = "k1" });
        _store.Put($"{Address}/1", Key(1));
        _store.Put($"{Current}/1", WithLayout(Key(7)));
    }

    async Task Because() => _report = await Migrate(remove: true);

    [Fact] void should_not_overwrite_it() => _store.DataOf($"{Current}/1").ShouldEqual(WithLayout(Key(7)));
    [Fact] void should_keep_it_as_a_conflict() => _report.ConflictsKept.ShouldEqual(1L);
    [Fact] void should_need_review() => _report.NeedsReview.ShouldBeTrue();
    [Fact] void should_not_remove_the_key_under_the_earlier_name() => _store.Has($"{Address}/1").ShouldBeTrue();
    [Fact] void should_not_remove_anything() => _report.LegacyRemoved.ShouldEqual(0L);
}
