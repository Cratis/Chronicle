// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_name;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultKeyStoreMigrationOnEmulator.given;

/// <summary>
/// Sets up the real <see cref="SecretClientVault"/> and <see cref="AzureKeyVaultKeyStoreMigrator"/> against the shared Azure Key Vault Emulator.
/// Each context gets a unique event store name to prevent collisions across specs.
/// </summary>
/// <param name="fixture">The <see cref="AzureKeyVaultFixture"/> providing the emulator container.</param>
public class a_migration_on_the_emulator(AzureKeyVaultFixture fixture) : IAsyncLifetime
{
    public static readonly EncryptionKey FirstKey = new([1, 2, 3], [4, 5, 6]);
    public static readonly EncryptionKey SecondKey = new([7, 8, 9], [10, 11, 12]);

    protected const string Namespace = "default";
    protected readonly EncryptionKeyIdentifier _identifier = "customer-42";
    protected EventStoreName _eventStore = default!;
    protected ISecretVault _vault = default!;
    protected List<LegacyScope> _scopes = default!;
    protected AzureKeyVaultEncryptionKeyStorage _storage = default!;

    protected AzureKeyVaultFixture Fixture => fixture;

    protected string Legacy(uint revision) => SecretLocations.For(_eventStore, Namespace, _identifier).LegacyName(revision)!;

    protected string Current(uint revision) => SecretLocations.For(_eventStore, Namespace, _identifier).Name(revision);

    protected string CurrentErasure => SecretLocations.For(_eventStore, Namespace, _identifier).ErasureName;

    protected string LegacyErasure => SecretLocations.For(_eventStore, Namespace, _identifier).LegacyErasureName!;

    /// <inheritdoc/>
    public virtual async ValueTask InitializeAsync()
    {
        _eventStore = $"mig-store-{Guid.NewGuid():N}";
        _scopes = [new LegacyScope(_eventStore.Value, Namespace)];
        _vault = new ScopedSecretVault(new SecretClientVault(fixture.SecretClient), $"chronicle--{_eventStore.Value}--");
        _storage = new AzureKeyVaultEncryptionKeyStorage(fixture.SecretClient);
        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;

    protected Task WriteLegacyKey(uint revision, EncryptionKey key) => LegacySecrets.WriteKey(fixture.SecretClient, Legacy(revision), key);

    protected Task<EncryptionKeyMigrationReport> Migrate(bool removeVerifiedLegacy = false) =>
        new AzureKeyVaultKeyStoreMigrator(_vault, _scopes, null).Migrate(false, removeVerifiedLegacy);
}
