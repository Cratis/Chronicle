// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreMigrator;

public class when_the_layout_cannot_be_proven : given.a_migrator
{
    EncryptionKeyMigrationReport _report;

    void Establish()
    {
        // Something already sits under the root of this version, without the marker that says this version put it there.
        _store.Put("k1/other/default/x/1", Key(1));
        _store.Put($"{Address}/1", Key(1));
    }

    async Task Because() => _report = await Migrate(remove: true);

    [Fact] void should_report_the_layout_as_unusable() => _report.LayoutUnusable.ShouldBeTrue();
    [Fact] void should_not_write_anything() => _store.Writes.ShouldEqual(0);
    [Fact] void should_not_remove_anything() => _store.Removes.ShouldEqual(0);
}
