// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using Azure;
using Azure.Security.KeyVault.Secrets;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault;

internal sealed partial class FencedAzureKeyVaultEncryptionKeyStorage
{
    const string DatabaseLifecycleTag = "chronicle-database-lifecycle";
    const string DatabaseVersionTag = "chronicle-database-version";

    static bool IsOlder(EncryptionKeyLegacyImport marker, KeyVaultSecret secret) =>
        marker.SeenVersions.Contains(secret.Properties.Version, StringComparer.Ordinal) || secret.Properties.CreatedOn < marker.CreatedOn;

    async Task<EncryptionKeyFenceState> ReadState(Address address)
    {
        for (var attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            var observed = await authority.ReadFor(address.EventStore, address.Namespace, address.AuthorityIdentifier);
            var next = observed;
            foreach (var name in new[] { address.Locations.ErasureName, address.Locations.LegacyErasureName }.OfType<string>())
            {
                var secret = await ReadLegacyFence(address, name);
                if (secret is not null) next = Import(address, next, secret);
            }

            if (ReferenceEquals(next, observed)) return observed;
            next = next with { Erasure = next.Erasure with { Version = checked(observed.Erasure.Version + 1) } };
            if (await authority.CompareExchangeFor(address.EventStore, address.Namespace, address.AuthorityIdentifier, observed, next)) return next;
            await Backoff(attempt);
        }

        throw new EncryptionKeyFenceUnavailable(address.Identifier);
    }

    async Task<KeyVaultSecret?> ReadLegacyFence(Address address, string name)
    {
        try
        {
            var secret = await TryRead(name);
            if (secret?.Properties.Enabled == false) throw new EncryptionKeyFenceUnavailable(address.Identifier);
            return secret;
        }
        catch (RequestFailedException)
        {
            throw new EncryptionKeyFenceUnavailable(address.Identifier);
        }
    }

    EncryptionKeyFenceState Import(Address address, EncryptionKeyFenceState state, KeyVaultSecret secret)
    {
        // Our compatibility writes contain only already-durable state. Reimporting them would make an
        // immediate authorization retry appear stale solely because its best-effort mirror succeeded.
        if (secret.Properties.Tags.TryGetValue(DatabaseVersionTag, out var databaseVersion) &&
            ulong.TryParse(databaseVersion, CultureInfo.InvariantCulture, out var mirroredVersion) && mirroredVersion <= state.Erasure.Version)
        {
            return state;
        }
        var marker = state.LegacyImports.FirstOrDefault(_ => _.Name.Equals(secret.Name, StringComparison.Ordinal));
        var version = secret.Properties.Version;
        if (string.IsNullOrEmpty(version)) throw new EncryptionKeyFenceUnavailable(address.Identifier);
        if (marker is not null && IsOlder(marker, secret)) return state;
        if ((marker?.SeenVersions.Count ?? 0) >= 1024) throw new EncryptionKeyFenceUnavailable(address.Identifier);
        var imported = ParseLegacyFence(address, secret.Value);
        var obsoleteDatabaseClosure = secret.Properties.Tags.TryGetValue(DatabaseLifecycleTag, out var raw) &&
            uint.TryParse(raw, CultureInfo.InvariantCulture, out var lifecycle) && lifecycle < state.Erasure.Lifecycle;
        var closes = !imported.NewKeyAllowed && !obsoleteDatabaseClosure;
        var erasure = state.Erasure with
        {
            ErasedThrough = new(Math.Max(state.Erasure.ErasedThrough.Value, imported.ErasedThrough.Value)),
            ErasedKeyFingerprints = state.Erasure.ErasedKeyFingerprints.Concat(imported.ErasedKeyFingerprints).Distinct(StringComparer.Ordinal).ToArray(),
            NewKeyAllowed = state.Erasure.NewKeyAllowed && !closes,
            ErasedLifecycle = closes ? Math.Max(state.Erasure.ErasedLifecycle, state.Erasure.Lifecycle) : state.Erasure.ErasedLifecycle
        };
        var updated = new EncryptionKeyLegacyImport(
            secret.Name,
            version,
            secret.Properties.CreatedOn,
            closes ? state.Erasure.Lifecycle : null,
            (marker?.SeenVersions ?? []).Append(version).ToArray());
        return state with { Erasure = erasure, LegacyImports = state.LegacyImports.Where(_ => !_.Name.Equals(secret.Name, StringComparison.Ordinal)).Append(updated).ToArray() };
    }

    EncryptionKeyErasure ParseLegacyFence(Address address, string value)
    {
        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(value);
            if (data is null || !data.TryGetValue("erasedThrough", out var through) || !uint.TryParse(through, CultureInfo.InvariantCulture, out var floor))
            {
                throw new EncryptionKeyFenceUnavailable(address.Identifier);
            }

            var fingerprints = data.GetValueOrDefault("erasedKeyFingerprints")?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [];
            var allowed = bool.TryParse(data.GetValueOrDefault("newKeyAllowed"), out var parsed) && parsed;
            return new(floor, fingerprints, allowed);
        }
        catch (JsonException)
        {
            throw new EncryptionKeyFenceUnavailable(address.Identifier);
        }
    }

    async Task WriteLegacyBestEffort(Address address, EncryptionKeyErasure fence)
    {
        var value = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["erasedThrough"] = fence.ErasedThrough.Value.ToString(CultureInfo.InvariantCulture),
            ["erasedKeyFingerprints"] = string.Join(',', fence.ErasedKeyFingerprints),
            ["newKeyAllowed"] = fence.NewKeyAllowed.ToString()
        });
        foreach (var name in new[] { address.Locations.ErasureName, address.Locations.LegacyErasureName }.OfType<string>())
        {
            try
            {
                var secret = new KeyVaultSecret(name, value);
                secret.Properties.Tags[DatabaseLifecycleTag] = fence.Lifecycle.ToString(CultureInfo.InvariantCulture);
                secret.Properties.Tags[DatabaseVersionTag] = fence.Version.ToString(CultureInfo.InvariantCulture);
                await client.SetSecretAsync(secret);
            }
            catch (Exception exception) when (exception is RequestFailedException or HttpRequestException or TaskCanceledException)
            {
                logger.LegacyWriteFailed(exception);
            }
        }
    }
}
