// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Vault.for_live_VaultForwardMigration;

[Collection(LiveVaultCollection.Name)]
public class when_migrating_verifying_and_running_again(LiveVaultFixture fixture) : given.a_live_migration(fixture)
{
    FaultingSecretStore _observed = default!;
    int? _migration;
    int? _verification;
    int? _second;
    string _afterFirst = default!;

    async Task Establish()
    {
        await Put(Legacy);
        await Put("other/default/customer-43/1", Key(2));
        _observed = new FaultingSecretStore(_store)
        {
            ObserveDeletion = async () => await HasPayload(Current, WithLayout(Key())) && await HasPayload("k1/other/default/customer-43/1", WithLayout(Key(2)))
        };
        _migrator = new VaultKeyStoreMigrator(_observed, NullLogger<VaultKeyStoreMigrator>.Instance);
    }

    async Task Because()
    {
        _migration = await ServerCommand.Run(_check, _migrator, LegacyKeyHandling.Refuse, "--migrate-encryption-keys", "--remove-migrated-legacy-keys");
        _verification = await ServerCommand.Run(_check, _migrator, LegacyKeyHandling.Refuse, "--verify-encryption-keys");
        _afterFirst = await Snapshot();
        _second = await ServerCommand.Run(_check, _migrator, LegacyKeyHandling.Refuse, "--migrate-encryption-keys", "--remove-migrated-legacy-keys");
    }

    [Fact] void should_migrate_with_a_clean_command_exit() => _migration.ShouldEqual(0);
    [Fact] void should_verify_with_a_clean_command_exit() => _verification.ShouldEqual(0);
    [Fact] void should_run_again_with_a_clean_command_exit() => _second.ShouldEqual(0);
    [Fact] void should_only_attempt_deletion_after_all_copies_are_verified() => _observed.AllDeletionsFollowedVerification.ShouldBeTrue();
    [Fact] void should_remove_exactly_the_verified_source_versions() => _observed.Deletions.ShouldEqual(2);
    [Fact] async Task should_leave_legacy_tombstones_rather_than_delete_metadata() => (await _store.IsTombstone(Legacy) && await _store.IsTombstone("other/default/customer-43/1")).ShouldBeTrue();
    [Fact] async Task should_not_mutate_payloads_or_metadata_on_the_second_run() => (_afterFirst == await Snapshot()).ShouldBeTrue();
}
