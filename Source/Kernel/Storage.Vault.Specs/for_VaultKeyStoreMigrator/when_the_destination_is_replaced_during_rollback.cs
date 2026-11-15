// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreMigrator;

public class when_the_destination_is_replaced_during_rollback : given.a_migrator
{
    EncryptionKeyMigrationReport _report;

    void Establish()
    {
        _store.Put($"{Address}/1", Key(1));
        _store.AfterWrite = path =>
        {
            if (path == $"{Current}/1") _store.Put($"{Address}/1", Key(2));
        };
        _store.BeforeRemove = path =>
        {
            if (path == $"{Current}/1") _store.Put(path, Key(9));
        };
    }

    async Task Because() => _report = await Migrate();

    [Fact] void should_preserve_the_destination_it_did_not_create() => _store.DataOf($"{Current}/1")["publicKey"].ShouldEqual(Key(9)["publicKey"]);
    [Fact] void should_keep_the_source() => _store.Has($"{Address}/1").ShouldBeTrue();
    [Fact] void should_report_the_interference() => _report.ConflictsKept.ShouldEqual(1L);
}
