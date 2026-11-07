// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.SecretsEngines;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreUpgradeCheck.given;

/// <summary>
/// Sets up a KV v2 mount of its own in the shared Vault container, so the layout of one spec never shows in another.
/// </summary>
/// <param name="fixture">The <see cref="VaultFixture"/> providing the Vault container.</param>
public class an_empty_mount(VaultFixture fixture) : IAsyncLifetime
{
    protected VaultFixture Fixture => fixture;

    protected string _mountPoint = default!;
    protected VaultClient _client = default!;
    protected for_VaultEncryptionKeyStorage.given.raw_vault _raw = default!;
    internal VaultKeyStoreUpgradeCheck _check = default!;

    /// <inheritdoc/>
    public virtual async ValueTask InitializeAsync()
    {
        _mountPoint = $"layout-{Guid.NewGuid():N}";
        _client = new VaultClient(new VaultClientSettings(fixture.VaultAddress, new TokenAuthMethodInfo(VaultFixture.RootToken)));
        await _client.V1.System.MountSecretBackendAsync(new SecretsEngine
        {
            Type = SecretsEngineType.KeyValueV2,
            Path = _mountPoint
        });
        _raw = new for_VaultEncryptionKeyStorage.given.raw_vault(fixture.VaultAddress, _mountPoint);
        _check = new VaultKeyStoreUpgradeCheck(_client, _mountPoint);
    }

    /// <inheritdoc/>
    public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
