// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_live_VaultForwardMigration;

[Collection(LiveVaultCollection.Name)]
public class when_a_marker_already_exists(LiveVaultFixture fixture) : given.a_live_migration(fixture)
{
    JsonNode? _marker;
    EncryptionKeyMigrationReport _report = default!;

    async Task Establish()
    {
        await Marker();
        await Put(Legacy);
        _marker = await Metadata(VaultPathSegments.LayoutMarker);
    }

    async Task Because() => _report = await Migrate();

    [Fact] void should_not_trust_a_marker_as_proof_of_migration() => _report.Verified.ShouldEqual(1L);
    [Fact] async Task should_copy_the_legacy_payload() => (await HasPayload(Current, WithLayout(Key()))).ShouldBeTrue();
    [Fact] async Task should_not_rewrite_the_marker() => JsonNode.DeepEquals(_marker, await Metadata(VaultPathSegments.LayoutMarker)).ShouldBeTrue();
}
