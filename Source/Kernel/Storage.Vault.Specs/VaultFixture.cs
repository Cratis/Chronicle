// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Cratis.Chronicle.Storage.Vault;

/// <summary>
/// Provides a shared HashiCorp Vault container for integration specs.
/// Starts Vault in development mode with a fixed root token and the KV v2 secrets engine
/// pre-mounted at <c>secret/</c>.
/// </summary>
public sealed class VaultFixture : IAsyncLifetime
{
    /// <summary>
    /// The root token used in development mode. Also set as the <c>VAULT_TOKEN</c>
    /// environment variable for each test run.
    /// </summary>
    public const string RootToken = "chronicle-test-root";

    const int VaultPort = 8200;

    IContainer? _container;

    /// <summary>
    /// Gets the Vault address to use when constructing <see cref="VaultEncryptionKeyStorage"/>.
    /// </summary>
    public string VaultAddress => $"http://localhost:{_container!.GetMappedPublicPort(VaultPort)}";

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        _container = new ContainerBuilder("hashicorp/vault:latest")
            .WithCommand(
                "vault",
                "server",
                "-dev",
                $"-dev-root-token-id={RootToken}",
                "-dev-listen-address=0.0.0.0:8200")
            .WithPortBinding(VaultPort, assignRandomHostPort: true)
            .WithEnvironment("VAULT_DEV_ROOT_TOKEN_ID", RootToken)
            .WithEnvironment("SKIP_SETCAP", "true")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(req => req.ForPort(VaultPort).ForPath("/v1/sys/health")))
            .Build();

        await _container.StartAsync();

        // Make the token available to VaultEncryptionKeyStorage via environment variable.
        Environment.SetEnvironmentVariable("VAULT_TOKEN", RootToken);
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
