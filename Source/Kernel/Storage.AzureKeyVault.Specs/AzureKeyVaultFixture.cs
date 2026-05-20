// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using AzureKeyVaultEmulator.TestContainers;
using Azure.Security.KeyVault.Secrets;

namespace Cratis.Chronicle.Storage.AzureKeyVault;

/// <summary>
/// Provides a shared Azure Key Vault Emulator container for integration specs.
/// Starts the emulator with automatic certificate generation and management.
/// </summary>
public sealed class AzureKeyVaultFixture : IAsyncLifetime
{
    AzureKeyVaultEmulatorContainer? _container;

    /// <summary>
    /// Gets a <see cref="SecretClient"/> configured to communicate with the emulator.
    /// </summary>
    public SecretClient SecretClient { get; private set; } = default!;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        var certDir = Path.Combine(Path.GetTempPath(), $"keyvaultemulator-{Guid.NewGuid():N}");
        Directory.CreateDirectory(certDir);

        _container = new AzureKeyVaultEmulatorContainer(
            certificatesDirectory: certDir,
            forceCleanupCertificates: true);

        await _container.StartAsync();

        SecretClient = _container.GetSecretClient();
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}
