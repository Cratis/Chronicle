// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_CompositeEncryptionKeyStorage.given;

/// <summary>
/// Sets up the cutover shape against a real Vault: Vault is the primary store and an in-memory store stands in
/// for the general storage backend a deployment is migrating away from.
/// Each context gets a unique event store name to prevent key collisions across tests.
/// </summary>
/// <param name="fixture">The <see cref="VaultFixture"/> providing the Vault container.</param>
public class a_composite_over_vault_and_the_default_storage(VaultFixture fixture) : IAsyncLifetime
{
    protected VaultEncryptionKeyStorage _vault = default!;
    protected InMemoryEncryptionKeyStorage _defaultStorage = default!;
    protected CompositeEncryptionKeyStorage _composite = default!;
    protected EventStoreName _eventStore = default!;
    protected EventStoreNamespaceName _namespace = default!;
    protected EncryptionKeyIdentifier _identifier = default!;

    /// <inheritdoc/>
    public virtual Task InitializeAsync()
    {
        _eventStore = $"test-store-{Guid.NewGuid():N}";
        _namespace = "default";
        _identifier = Guid.NewGuid().ToString();
        _vault = new VaultEncryptionKeyStorage(fixture.VaultAddress);
        _defaultStorage = new InMemoryEncryptionKeyStorage();
        _composite = new CompositeEncryptionKeyStorage(_vault, _defaultStorage);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public virtual Task DisposeAsync() => Task.CompletedTask;
}
