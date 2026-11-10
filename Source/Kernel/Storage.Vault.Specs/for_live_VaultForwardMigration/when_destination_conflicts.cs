// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_live_VaultForwardMigration;

[Collection(LiveVaultCollection.Name)]
public class when_destination_conflicts(LiveVaultFixture fixture) : given.a_live_migration(fixture)
{
    EncryptionKeyMigrationReport _report = default!;

    async Task Establish()
    {
        await Marker();
        await Put(Legacy);
        await Put(Current, WithLayout(Key(2)));
    }

    async Task Because() => _report = await Migrate(remove: true);

    [Fact] void should_require_review() => _report.ConflictsKept.ShouldEqual(1L);
    [Fact] void should_not_delete_legacy() => _report.LegacyRemoved.ShouldEqual(0L);
    [Fact] async Task should_not_overwrite_destination() => (await HasPayload(Current, WithLayout(Key(2)))).ShouldBeTrue();
    [Fact] async Task should_keep_source_material() => (await HasPayload(Legacy, Key())).ShouldBeTrue();
}
