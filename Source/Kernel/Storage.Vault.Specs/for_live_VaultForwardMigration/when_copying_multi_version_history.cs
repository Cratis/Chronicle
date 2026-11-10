// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_live_VaultForwardMigration;

[Collection(LiveVaultCollection.Name)]
public class when_copying_multi_version_history(LiveVaultFixture fixture) : given.a_live_migration(fixture)
{
    JsonNode? _before;
    EncryptionKeyMigrationReport _report = default!;

    async Task Establish()
    {
        await Put(Legacy);
        await Put(Legacy, Key(2));
        _before = await Metadata(Legacy);
    }

    async Task Because() => _report = await Migrate();

    [Fact] void should_verify_the_latest_current_payload() => _report.Verified.ShouldEqual(1L);
    [Fact] async Task should_copy_only_the_latest_payload() => (await HasPayload(Current, WithLayout(Key(2)))).ShouldBeTrue();
    [Fact] async Task should_keep_legacy_version_metadata_intact() => JsonNode.DeepEquals(_before, await Metadata(Legacy)).ShouldBeTrue();
    [Fact] async Task should_keep_the_earlier_payload_readable_under_legacy_names() => (await VersionHasPayload(Legacy, 1, Key())).ShouldBeTrue();
    [Fact] async Task should_keep_the_latest_legacy_payload() => (await VersionHasPayload(Legacy, 2, Key(2))).ShouldBeTrue();
    [Fact] async Task should_not_claim_to_copy_source_version_numbers() => (await Metadata(Current))!["current_version"]!.GetValue<int>().ShouldEqual(1);
}
