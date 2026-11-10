// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreMigrator;

public class when_the_key_store_cannot_be_reached : given.a_migrator
{
    EncryptionKeyMigrationReport _report;

    void Establish() => _store.FailEverything = new HttpRequestException("unreachable");

    async Task Because() => _report = await Migrate();

    [Fact] void should_report_the_layout_as_unusable() => _report.LayoutUnusable.ShouldBeTrue();
    [Fact] void should_not_write_anything() => _store.Writes.ShouldEqual(0);
}
