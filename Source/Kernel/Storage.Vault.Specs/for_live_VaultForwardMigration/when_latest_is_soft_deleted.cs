// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_live_VaultForwardMigration;

[Collection(LiveVaultCollection.Name)]
public class when_latest_is_soft_deleted(LiveVaultFixture fixture) : given.a_live_migration(fixture)
{
    JsonNode? _before;
    EncryptionKeyMigrationReport _report = default!;

    async Task Establish()
    {
        await Put(Legacy);
        await Put(Legacy, Key(2));
        await DeleteVersions(Legacy, 2);
        _before = await Metadata(Legacy);
    }

    async Task Because() => _report = await Migrate(remove: true);

    [Fact] void should_require_review_of_the_unreadable_latest() => _report.ConflictsKept.ShouldEqual(1L);
    [Fact] void should_not_delete_legacy_history() => _report.LegacyRemoved.ShouldEqual(0L);
    [Fact] async Task should_not_resurrect_an_older_value_at_destination() => (await _store.Read(Current) is null).ShouldBeTrue();
    [Fact] async Task should_preserve_deleted_version_metadata() => JsonNode.DeepEquals(_before, await Metadata(Legacy)).ShouldBeTrue();
    [Fact] async Task should_preserve_the_older_readable_version() => (await VersionHasPayload(Legacy, 1, Key())).ShouldBeTrue();
}
