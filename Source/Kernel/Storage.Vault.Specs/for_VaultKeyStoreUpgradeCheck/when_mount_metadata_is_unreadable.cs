// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Storage.Compliance;
using VaultSharp;
using VaultSharp.Core;
using VaultSharp.V1.Commons;
using VaultSharp.V1.SecretsEngines;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreUpgradeCheck;

public class when_mount_metadata_is_unreadable : Specification
{
    IVaultClient _client;
    EncryptionKeyStoreUpgradeStatus _startup;
    EncryptionKeyStoreUpgradeStatus _fullInspection;
    EncryptionKeyMigrationReport _migration;
    EncryptionKeyMigrationReport _dryRun;

    void Establish()
    {
        _client = Substitute.For<IVaultClient>();
        _client.V1.System.GetSecretBackendAsync("secret")
            .Returns(Task.FromException<Secret<SecretsEngine>>(new VaultApiException(HttpStatusCode.Forbidden, "permission denied")));
        _client.V1.Secrets.KeyValue.V2.ReadSecretAsync(VaultPathSegments.LayoutMarker, mountPoint: "secret")
            .Returns(new Secret<SecretData>
            {
                Data = new SecretData
                {
                    Data = new Dictionary<string, object> { ["format"] = VaultPathSegments.Root },
                    Metadata = new CurrentSecretMetadata { Version = 1 }
                }
            });
        _client.V1.Secrets.KeyValue.V2.ReadSecretPathsAsync(string.Empty, mountPoint: "secret")
            .Returns(new Secret<ListInfo> { Data = new ListInfo { Keys = [] } });
    }

    async Task Because()
    {
        var check = new VaultKeyStoreUpgradeCheck(_client);
        _startup = await check.Inspect(false);
        _fullInspection = await check.Inspect(true);
        var migrator = new VaultKeyStoreMigrator(_client);
        _migration = await migrator.Migrate(false, false);
        _dryRun = await migrator.Migrate(true, false);
    }

    [Fact] void should_allow_default_startup_inspection() => _startup.LayoutUnusable.ShouldBeFalse();
    [Fact] void should_allow_full_inspection_for_refuse() => _fullInspection.LayoutUnusable.ShouldBeFalse();
    [Fact] void should_refuse_writing_migration() => _migration.LayoutUnusable.ShouldBeTrue();
    [Fact] void should_refuse_migration_dry_run() => _dryRun.LayoutUnusable.ShouldBeTrue();
    [Fact] async Task should_only_read_mount_metadata_for_migration() => await _client.V1.System.Received(2).GetSecretBackendAsync("secret");
    [Fact] async Task should_never_write() => await _client.V1.Secrets.KeyValue.V2.DidNotReceive().WriteSecretAsync(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>(), Arg.Any<int?>(), Arg.Any<string>());
}
