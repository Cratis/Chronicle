// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.SecretsEngines;

namespace Cratis.Chronicle.Storage.Vault.for_EncryptionKeyStorageContract;

/// <summary>
/// Real Vault contract, CI only. Each fact uses a fresh mount in the version-pinned collection fixture.
/// </summary>
/// <param name="fixture">The shared pinned Vault server.</param>
[Collection(VaultCollection.Name)]
public class on_server(VaultFixture fixture) : VaultEncryptionKeyStorageContract
{
    protected override IEncryptionKeyStorage CreateStorage()
    {
        Mount = $"fence-{Guid.NewGuid():N}";
        var settings = new VaultClientSettings(fixture.VaultAddress, new TokenAuthMethodInfo(VaultFixture.RootToken))
        {
            MyHttpClientProviderFunc = handler =>
            {
                Interleaving = new(handler);
                return new HttpClient(Interleaving);
            }
        };
        Client = new VaultClient(settings);
        Client.V1.System.MountSecretBackendAsync(new SecretsEngine { Type = SecretsEngineType.KeyValueV2, Path = Mount }).GetAwaiter().GetResult();
        return Storage = new VaultEncryptionKeyStorage(Client, Mount, random: new Random(17), delay: _ => Task.CompletedTask);
    }
}
