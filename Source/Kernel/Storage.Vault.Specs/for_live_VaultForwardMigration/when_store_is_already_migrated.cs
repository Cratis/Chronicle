// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_live_VaultForwardMigration;

[Collection(LiveVaultCollection.Name)]
public class when_store_is_already_migrated(LiveVaultFixture fixture) : given.a_live_migration(fixture)
{
    string _before = default!;
    EncryptionKeyMigrationReport _report = default!;

    async Task Establish()
    {
        await Marker();
        await Put(Current, WithLayout(Key()));
        _before = await Snapshot();
    }

    async Task Because() => _report = await Migrate();

    [Fact] void should_report_a_clean_no_op() => (_report.Complete && !_report.NeedsReview && _report.Copied == 0 && _report.Failed == 0).ShouldBeTrue();
    [Fact] async Task should_not_change_any_version_or_metadata() => (_before == await Snapshot()).ShouldBeTrue();
}
