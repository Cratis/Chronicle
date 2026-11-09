// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_live_VaultForwardMigration;

[Collection(LiveVaultCollection.Name)]
public class when_migrating_an_empty_store(LiveVaultFixture fixture) : given.a_live_migration(fixture)
{
    EncryptionKeyMigrationReport _report = default!;

    async Task Because() => _report = await Migrate();

    [Fact] void should_finish_cleanly() => (_report.Complete && !_report.NeedsReview && _report.Failed == 0).ShouldBeTrue();
    [Fact] void should_not_find_material() => _report.Scanned.ShouldEqual(0L);
    [Fact] async Task should_record_the_layout() => (await HasPayload(VaultPathSegments.LayoutMarker, new() { ["format"] = "k1" })).ShouldBeTrue();
}
