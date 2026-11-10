// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_live_VaultForwardMigration;

[Collection(LiveVaultCollection.Name)]
public class when_resuming_a_partial_migration(LiveVaultFixture fixture) : given.a_live_migration(fixture)
{
    EncryptionKeyMigrationReport _report = default!;
    JsonNode? _priorCopy;

    async Task Establish()
    {
        await Marker();
        await Put(Legacy);
        await Put("store/default/customer-43/1", Key(2));
        await Put(Current, WithLayout(Key()));
        _priorCopy = await Metadata(Current);
    }

    async Task Because() => _report = await Migrate();

    [Fact] void should_reverify_the_prior_copy() => _report.AlreadyMigrated.ShouldEqual(1L);
    [Fact] void should_copy_the_remaining_entry() => _report.Copied.ShouldEqual(1L);
    [Fact] void should_verify_both_entries() => _report.Verified.ShouldEqual(2L);
    [Fact] async Task should_not_add_a_version_to_the_prior_copy() => JsonNode.DeepEquals(_priorCopy, await Metadata(Current)).ShouldBeTrue();
    [Fact] async Task should_copy_the_remaining_payload() => (await HasPayload("k1/store/default/customer-43/1", WithLayout(Key(2)))).ShouldBeTrue();
}
