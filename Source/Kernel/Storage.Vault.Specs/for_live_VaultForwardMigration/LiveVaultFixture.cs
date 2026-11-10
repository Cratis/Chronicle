// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;

namespace Cratis.Chronicle.Storage.Vault.for_live_VaultForwardMigration;

/// <summary>
/// Owns a throwaway, pinned dev-mode Vault; authentication never changes the process environment.
/// </summary>
public sealed class LiveVaultFixture : IAsyncLifetime
{
    const int Port = 8200;
    readonly string _token = Guid.NewGuid().ToString("N");
    IContainer _container = default!;

    /// <summary>
    /// Creates a client authenticated only for this fixture.
    /// </summary>
    /// <returns>The Vault client.</returns>
    public IVaultClient CreateClient() => new VaultClient(new VaultClientSettings(Address, new TokenAuthMethodInfo(_token)));

    /// <summary>
    /// Creates an authenticated HTTP client for native KV-v2 fixture operations.
    /// </summary>
    /// <returns>The HTTP client, owned by the caller.</returns>
    public HttpClient CreateHttpClient()
    {
        var client = new HttpClient { BaseAddress = new Uri(Address) };
        client.DefaultRequestHeaders.Add("X-Vault-Token", _token);
        return client;
    }

    string Address => $"http://{_container.Hostname}:{_container.GetMappedPublicPort(Port)}";

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        _container = new ContainerBuilder("hashicorp/vault:1.18.3")
            .WithCommand("vault", "server", "-dev", $"-dev-root-token-id={_token}", "-dev-listen-address=0.0.0.0:8200")
            .WithPortBinding(Port, assignRandomHostPort: true)
            .WithEnvironment("SKIP_SETCAP", "true")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPort(Port).ForPath("/v1/sys/health")))
            .Build();
        await _container.StartAsync();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}
