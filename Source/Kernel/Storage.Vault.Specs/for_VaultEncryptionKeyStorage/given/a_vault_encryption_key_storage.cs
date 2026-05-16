// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.given;

/// <summary>
/// Sets up a <see cref="VaultEncryptionKeyStorage"/> connected to the shared Vault container.
/// Each context gets a unique event store name to prevent key collisions across tests.
/// </summary>
/// <param name="fixture">The <see cref="VaultFixture"/> providing the Vault container.</param>
public class a_vault_encryption_key_storage(VaultFixture fixture) : IAsyncLifetime
{
    protected VaultEncryptionKeyStorage _storage = default!;
    protected EventStoreName _eventStore = default!;
    protected EventStoreNamespaceName _namespace = default!;

    /// <inheritdoc/>
    public virtual Task InitializeAsync()
    {
        _eventStore = $"test-store-{Guid.NewGuid():N}";
        _namespace = "default";
        _storage = new VaultEncryptionKeyStorage(fixture.VaultAddress);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public virtual Task DisposeAsync() => Task.CompletedTask;
}
