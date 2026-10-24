// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Azure.Core.Pipeline;
using Azure.Security.KeyVault.Secrets;
using AzureKeyVaultEmulator.TestContainers;

namespace Cratis.Chronicle.Storage.AzureKeyVault;

/// <summary>
/// Provides a shared Azure Key Vault Emulator container for integration specs.
/// Starts the emulator with automatic certificate generation and management.
/// </summary>
#pragma warning disable CA1001 // Cleanup handled in DisposeAsync
public sealed class AzureKeyVaultFixture : IAsyncLifetime
#pragma warning restore CA1001
{
    AzureKeyVaultEmulatorContainer? _container;
    X509Certificate2? _trustedRoot;
    SocketsHttpHandler? _handler;
    HttpClient? _httpClient;

    /// <summary>
    /// Gets a <see cref="SecretClient"/> configured to communicate with the emulator.
    /// </summary>
    public SecretClient SecretClient { get; private set; } = default!;

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        var certDir = Path.Combine(Path.GetTempPath(), $"keyvaultemulator-{Guid.NewGuid():N}");
        Directory.CreateDirectory(certDir);

        var options = new AzureKeyVaultEmulatorOptions
        {
            LocalCertificatePath = certDir,
            LoadCertificatesIntoTrustStore = false
        };
        _container = new AzureKeyVaultEmulatorContainer(options);

        // The package resolves LocalCertificatePath to the temporary directory in CI.
        _trustedRoot = X509CertificateLoader.LoadCertificateFromFile(Path.Combine(options.LocalCertificatePath, "emulator.crt"));
        _handler = new SocketsHttpHandler();
        _handler.SslOptions.CertificateChainPolicy = EmulatorCertificateValidation.CreatePolicy(_trustedRoot);
        _httpClient = new HttpClient(_handler, disposeHandler: false);

        await _container.StartAsync();

        var endpoint = _container.GetEndpoint();
        var token = await _httpClient.GetStringAsync(new Uri($"{endpoint}/token"));
        SecretClient = new SecretClient(new Uri(endpoint), new EmulatorTokenCredential(token), new SecretClientOptions
        {
            DisableChallengeResourceVerification = true,
            Transport = new HttpClientTransport(_httpClient)
        });
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_container is not null)
            {
                await _container.DisposeAsync();
            }
        }
        finally
        {
            _httpClient?.Dispose();
            _handler?.Dispose();
            _trustedRoot?.Dispose();
        }
    }
}
