// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Azure;
using Azure.Security.KeyVault.Secrets;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.AzureKeyVault;

/// <summary>Stages material in Key Vault and publishes its exact version in a database CAS.</summary>
/// <param name="client">The material service.</param>
/// <param name="authority">The database authority.</param>
/// <param name="legacy">The unchanged legacy reader.</param>
/// <param name="logger">The logger.</param>
/// <param name="delay">The retry delay.</param>
internal sealed partial class FencedAzureKeyVaultEncryptionKeyStorage(
    SecretClient client,
    IEncryptionKeyFence authority,
    LegacyAzureKeyVaultEncryptionKeyStorage legacy,
    ILogger<AzureKeyVaultEncryptionKeyStorage> logger,
    Func<TimeSpan, Task>? delay) : IEncryptionKeyStorage
{
    /// <summary>The immutable staging lifecycle tag.</summary>
    internal const string StagedLifecycleTag = "chronicle-staged-lifecycle";
    const int MaximumAttempts = 8;
    const string FingerprintTag = "chronicle-key-fingerprint";

    /// <inheritdoc/>
    public EncryptionKeyFenceGuarantees FenceGuarantees => EncryptionKeyFenceGuarantees.MonotonicFence | EncryptionKeyFenceGuarantees.FencedSave;

    /// <inheritdoc/>
    public async Task SaveFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key, EncryptionKeyRevision? revision = null) =>
        _ = await StageAndPublish(await AddressFor(eventStore, eventStoreNamespace, identifier), key, revision, provision: false);

    /// <inheritdoc/>
    public async Task<EncryptionKey> GetOrAddFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key) =>
        (await GetOrAddWithRevisionFor(eventStore, eventStoreNamespace, identifier, key)).Key;

    /// <inheritdoc/>
    public async Task<EncryptionKeyAtRevision> GetOrAddWithRevisionFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key)
    {
        var address = await AddressFor(eventStore, eventStoreNamespace, identifier);
        return await ReadLatest(address) ?? await StageAndPublish(address, key, null, provision: true);
    }

    /// <inheritdoc/>
    public async Task<EncryptionKeyAtRevision?> TryGetLatestFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        await ReadLatest(await AddressFor(eventStore, eventStoreNamespace, identifier));

    /// <inheritdoc/>
    public async Task<bool> HasFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null) =>
        await TryGetFor(eventStore, eventStoreNamespace, identifier, revision) is not null;

    /// <inheritdoc/>
    public async Task<EncryptionKey?> TryGetFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null)
    {
        var address = await AddressFor(eventStore, eventStoreNamespace, identifier);
        return IsLatest(revision) ? (await ReadLatest(address))?.Key : (await ReadRevision(address, await ReadState(address), revision!))?.Key;
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey> GetFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null) =>
        await TryGetFor(eventStore, eventStoreNamespace, identifier, revision) ?? throw new MissingEncryptionKey(identifier);

    static bool IsLatest(EncryptionKeyRevision? revision) => revision is null || revision == EncryptionKeyRevision.Latest;

    async Task<EncryptionKeyAtRevision> StageAndPublish(Address address, EncryptionKey key, EncryptionKeyRevision? revision, bool provision)
    {
        var observed = await ReadState(address);
        var actual = IsLatest(revision) ? await NextRevision(address, observed) : revision!;
        observed.Erasure.EnsureCanSave(address.Identifier, actual, key);
        observed.Erasure.EnsureStillAllowed(address.Identifier, new(key, actual, observed.Erasure.Lifecycle));
        var staged = new KeyVaultSecret(address.Locations.Name(actual), LegacyAzureKeyVaultEncryptionKeyStorage.Serialize(key));
        staged.Properties.Tags[StagedLifecycleTag] = observed.Erasure.Lifecycle.ToString(CultureInfo.InvariantCulture);
        staged.Properties.Tags[FingerprintTag] = key.Fingerprint;
        var written = (await client.SetSecretAsync(staged)).Value;
        var publication = new EncryptionKeyPublication(actual.Value, written.Name, written.Properties.Version, observed.Erasure.Lifecycle, key.Fingerprint);
        if (string.IsNullOrEmpty(publication.Version)) throw new EncryptionKeyFenceUnavailable(address.Identifier);
        try
        {
            return await Publish(address, observed.Erasure.Lifecycle, key, publication, provision);
        }
        catch (Exception exception) when (exception is EncryptionKeyErased or EncryptionKeyFenceChanged)
        {
            await DisableOwnedVersion(publication, address.Identifier);
            throw;
        }
    }

    async Task<EncryptionKeyAtRevision> Publish(Address address, uint lifecycle, EncryptionKey key, EncryptionKeyPublication publication, bool provision)
    {
        for (var attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            var current = await ReadState(address);
            current.Erasure.EnsureStillAllowed(address.Identifier, new(key, new(publication.Revision), lifecycle));
            if (current.Erasure.Lifecycle != lifecycle) throw new EncryptionKeyFenceChanged(address.Identifier);
            if (provision && await ReadLatest(address) is { } winner)
            {
                await DisableOwnedVersion(publication, address.Identifier);
                return winner;
            }

            if (await authority.PublishRevision(address.EventStore, address.Namespace, address.AuthorityIdentifier, current, publication))
            {
                return new(key, new(publication.Revision), lifecycle);
            }

            await Backoff(attempt);
        }

        throw new EncryptionKeyFenceUnavailable(address.Identifier);
    }

    async Task DisableOwnedVersion(EncryptionKeyPublication publication, EncryptionKeyIdentifier identifier)
    {
        try
        {
            var properties = (await client.GetSecretAsync(publication.Name, publication.Version)).Value.Properties;
            properties.Enabled = false;
            await client.UpdateSecretPropertiesAsync(properties);
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            // An explicit bounded erasure already removed this writer's version.
        }
        catch (RequestFailedException)
        {
            throw new EncryptionKeyFenceUnavailable(identifier);
        }
    }

    async Task<EncryptionKeyAtRevision?> ReadLatest(Address address)
    {
        var state = await ReadState(address);
        var revisions = state.Publications.Select(_ => _.Revision).ToHashSet();
        foreach (var secret in await legacy.ListSecrets(address.Locations)) revisions.Add(secret.Revision.Value);
        foreach (var revision in revisions.OrderDescending())
        {
            if (await ReadRevision(address, state, new(revision)) is { } key) return key;
        }

        return null;
    }

    async Task<EncryptionKeyAtRevision?> ReadRevision(Address address, EncryptionKeyFenceState state, EncryptionKeyRevision revision)
    {
        if (state.Publications.FirstOrDefault(_ => _.Revision == revision.Value) is { } published)
        {
            if (published.Deleted || state.Covers(published)) return null;
            var secret = await TryRead(published.Name, published.Version);
            if (secret is null || secret.Properties.Enabled == false) throw new EncryptionKeyFenceUnavailable(address.Identifier);
            var key = LegacyAzureKeyVaultEncryptionKeyStorage.Deserialize(secret.Value) ?? throw new EncryptionKeyFenceUnavailable(address.Identifier);
            if (!key.Fingerprint.Equals(published.Fingerprint, StringComparison.Ordinal)) throw new EncryptionKeyFenceUnavailable(address.Identifier);
            return new(key, revision, published.Lifecycle);
        }

        if (revision.Value <= state.Erasure.ErasedThrough.Value) return null;
        var material = await ReadLegacy(address.Locations.Name(revision)) ??
            (address.Locations.LegacyName(revision) is { } name ? await ReadLegacy(name) : null);
        var pair = material is null ? null : new EncryptionKeyAtRevision(material, revision);
        return pair is not null && !state.Erasure.Covers(pair) ? pair : null;
    }

    async Task<EncryptionKey?> ReadLegacy(string name)
    {
        var latest = await TryRead(name);
        if (latest is null) return null;
        if (!latest.Properties.Tags.ContainsKey(StagedLifecycleTag)) return await legacy.ReadKey(name);
        var versions = new List<SecretProperties>();
        await foreach (var properties in client.GetPropertiesOfSecretVersionsAsync(name))
        {
            if (!properties.Tags.ContainsKey(StagedLifecycleTag)) versions.Add(properties);
        }

        if (versions.Exists(_ => _.Enabled == false)) throw new EncryptionKeyUnavailable();
        var previous = versions.OrderByDescending(_ => _.CreatedOn).ThenByDescending(_ => _.Version, StringComparer.Ordinal).FirstOrDefault();
        if (previous is null) return null;
        if (ProvisioningCandidateElection.IsCandidate(previous))
        {
            return await new ProvisioningCandidateElection(client, TimeProvider.System, LegacyAzureKeyVaultEncryptionKeyStorage.Deserialize).Elect(name);
        }

        return LegacyAzureKeyVaultEncryptionKeyStorage.Deserialize((await client.GetSecretAsync(name, previous.Version)).Value.Value);
    }

    async Task<EncryptionKeyRevision> NextRevision(Address address, EncryptionKeyFenceState state)
    {
        var revisions = (await legacy.ListSecrets(address.Locations)).Select(_ => _.Revision.Value)
            .Concat(state.Publications.Select(_ => _.Revision)).Append(state.Erasure.ErasedThrough.Value);
        return new(checked(revisions.Max() + 1));
    }

    async Task<KeyVaultSecret?> TryRead(string name, string? version = null)
    {
        try { return (await client.GetSecretAsync(name, version)).Value; }
        catch (RequestFailedException exception) when (exception.Status == 404) { return null; }
    }

    async Task<Address> AddressFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        var locations = await legacy.Resolve(eventStore, eventStoreNamespace, identifier);
        var scope = $"{client.VaultUri}|{locations.Prefix}";
        return new(eventStore, eventStoreNamespace, identifier, locations, new(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(scope)))));
    }

    Task Backoff(int attempt) => (delay ?? Task.Delay)(TimeSpan.FromMilliseconds(Random.Shared.Next(1 << Math.Min(attempt, 6), 2 << Math.Min(attempt, 6))));

    sealed record Address(EventStoreName EventStore, EventStoreNamespaceName Namespace, EncryptionKeyIdentifier Identifier, SecretLocations Locations, EncryptionKeyIdentifier AuthorityIdentifier);
}
