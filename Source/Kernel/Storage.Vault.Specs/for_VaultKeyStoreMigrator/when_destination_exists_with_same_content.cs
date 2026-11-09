// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreMigrator;

public class when_destination_exists_with_same_content : given.a_migrator
{
    EncryptionKeyMigrationReport _report;

    void Establish()
    {
        _store.Put(VaultPathSegments.LayoutMarker, new Dictionary<string, string> { ["format"] = "k1" });
        _store.Put($"{Address}/1", Key(1));
        _store.Put($"{Current}/1", WithLayout(Key(1)));
    }

    async Task Because() => _report = await Migrate();

    [Fact] void should_count_it_as_already_migrated() => _report.AlreadyMigrated.ShouldEqual(1L);
    [Fact] void should_write_nothing() => _store.Writes.ShouldEqual(0);
    [Fact] void should_verify_it() => _report.Verified.ShouldEqual(1L);
}
