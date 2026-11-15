// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Vault.for_live_VaultForwardMigration;

[Collection(LiveVaultCollection.Name)]
public class when_interrupted_with_a_lock(LiveVaultFixture fixture) : given.a_live_migration(fixture)
{
    EncryptionKeyMigrationReport _competing = default!;
    bool _interrupted;
    EncryptionKeyMigrationReport _blocked = default!;
    EncryptionKeyMigrationReport _recovered = default!;
    bool _lockRetained;
    bool _legacyRetained;

    async Task Establish() => await Put(Legacy);

    async Task Because()
    {
        using var interruption = new CancellationTokenSource();
        var fault = new FaultingSecretStore(_store)
        {
            InterruptAfterCopy = true,
            FailLockRelease = true,
            Interruption = interruption.Token
        };
        var runner = new VaultKeyStoreMigrator(fault, NullLogger<VaultKeyStoreMigrator>.Instance);
        var active = runner.Migrate(false, true, interruption.Token);
        try
        {
            await fault.CopyWritten.Task.WaitAsync(TimeSpan.FromSeconds(20));
            _competing = await Migrate();
        }
        finally
        {
            await interruption.CancelAsync();
            try
            {
                await active.WaitAsync(TimeSpan.FromSeconds(20));
            }
            catch (OperationCanceledException) when (interruption.IsCancellationRequested)
            {
                _interrupted = true;
            }
        }

        _lockRetained = await _store.Read(VaultKeyStoreMigrator.MigrationLockPath) is not null;
        _legacyRetained = await HasPayload(Legacy, Key());
        _blocked = await Migrate();

        // Explicit operator recovery, only AFTER the interrupted owner has finished.
        // No expiry/stealing, production change, or rollback rehearsal is implied.
        await _store.Remove(VaultKeyStoreMigrator.MigrationLockPath);
        _recovered = await Migrate();
    }

    [Fact] void should_refuse_a_competing_owner() => _competing.MigrationLocked.ShouldBeTrue();
    [Fact] void should_report_the_interruption() => _interrupted.ShouldBeTrue();
    [Fact] void should_keep_the_orphaned_lock_when_release_is_interrupted() => _lockRetained.ShouldBeTrue();
    [Fact] void should_not_steal_an_orphaned_lock() => _blocked.MigrationLocked.ShouldBeTrue();
    [Fact] void should_keep_legacy_before_recovery() => _legacyRetained.ShouldBeTrue();
    [Fact] void should_reverify_the_acknowledged_copy_after_operator_recovery() => (_recovered.Complete && _recovered.AlreadyMigrated == 1 && _recovered.Verified == 1 && _recovered.Failed == 0).ShouldBeTrue();
    [Fact] async Task should_release_the_recovered_lock() => (await _store.Read(VaultKeyStoreMigrator.MigrationLockPath) is null).ShouldBeTrue();
}
