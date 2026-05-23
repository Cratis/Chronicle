// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.given;

/// <summary>
/// Sets up an <see cref="AzureKeyVaultEncryptionKeyStorage"/> connected to the shared Azure Key Vault Emulator container.
/// Each context gets a unique event store name to prevent key collisions across tests.
/// </summary>
/// <param name="fixture">The <see cref="AzureKeyVaultFixture"/> providing the emulator container.</param>
public class a_azure_key_vault_encryption_key_storage(AzureKeyVaultFixture fixture) : IAsyncLifetime
{
    protected AzureKeyVaultEncryptionKeyStorage _storage = default!;
    protected EventStoreName _eventStore = default!;
    protected EventStoreNamespaceName _namespace = default!;

    /// <inheritdoc/>
    public virtual Task InitializeAsync()
    {
        _eventStore = $"test-store-{Guid.NewGuid():N}";
        _namespace = "default";
        _storage = new AzureKeyVaultEncryptionKeyStorage(fixture.SecretClient);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public virtual Task DisposeAsync() => Task.CompletedTask;
}
