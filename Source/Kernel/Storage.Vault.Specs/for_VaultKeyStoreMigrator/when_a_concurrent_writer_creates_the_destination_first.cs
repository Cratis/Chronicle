// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreMigrator;

public class when_a_concurrent_writer_creates_the_destination_first : given.a_migrator
{
    EncryptionKeyMigrationReport _report;

    void Establish()
    {
        _store.Put($"{Address}/1", Key(1));
        _store.BeforeWrite = path =>
        {
            if (path == $"{Current}/1")
            {
                _store.Put(path, WithLayout(Key(7)));
            }
        };
    }

    async Task Because() => _report = await Migrate();

    [Fact] void should_keep_what_the_other_writer_wrote() => _store.DataOf($"{Current}/1").ShouldEqual(WithLayout(Key(7)));
    [Fact] void should_keep_it_as_a_conflict() => _report.ConflictsKept.ShouldEqual(1L);
    [Fact] void should_not_count_it_as_copied() => _report.Copied.ShouldEqual(0L);
}
