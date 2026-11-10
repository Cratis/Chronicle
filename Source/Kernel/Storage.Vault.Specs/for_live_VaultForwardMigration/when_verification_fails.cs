// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Vault.for_live_VaultForwardMigration;

[Collection(LiveVaultCollection.Name)]
public class when_verification_fails(LiveVaultFixture fixture) : given.a_live_migration(fixture)
{
    FaultingSecretStore _fault = default!;
    EncryptionKeyMigrationReport _report = default!;

    async Task Establish()
    {
        await Put(Legacy);
        _fault = new FaultingSecretStore(_store) { FailReadback = true };
        _migrator = new VaultKeyStoreMigrator(_fault, NullLogger<VaultKeyStoreMigrator>.Instance);
    }

    async Task Because() => _report = await Migrate(remove: true);

    [Fact] void should_report_failed_verification() => (_report.Failed > 0 && _report.Verified == 0).ShouldBeTrue();
    [Fact] void should_not_attempt_deletion() => _fault.Deletions.ShouldEqual(0);
    [Fact] void should_not_report_removed_legacy() => _report.LegacyRemoved.ShouldEqual(0L);
    [Fact] async Task should_keep_the_legacy_payload() => (await HasPayload(Legacy, Key())).ShouldBeTrue();
    [Fact] async Task should_have_written_a_real_copy_before_injecting_the_failed_read() => (await HasPayload(Current, WithLayout(Key()))).ShouldBeTrue();
}
