// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Storage.Vault.for_live_VaultForwardMigration;

[Collection(LiveVaultCollection.Name)]
public class when_history_contains_destroyed_versions(LiveVaultFixture fixture) : given.a_live_migration(fixture)
{
    JsonNode? _before;

    async Task Establish()
    {
        await Put(Legacy);
        await Put(Legacy, Key(2));
        await DestroyVersions(Legacy, 1);
        _before = await Metadata(Legacy);
    }

    async Task Because() => await Migrate();

    [Fact] async Task should_copy_the_live_latest_value() => (await HasPayload(Current, WithLayout(Key(2)))).ShouldBeTrue();
    [Fact] async Task should_keep_destroyed_version_metadata_intact() => JsonNode.DeepEquals(_before, await Metadata(Legacy)).ShouldBeTrue();
    [Fact] async Task should_not_make_the_destroyed_version_readable() => ((await Version(Legacy, 1))?["data"] is null).ShouldBeTrue();
}
