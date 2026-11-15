// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.AzureKeyVault.for_EncryptionKeyStorageContract.given;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_name;

public class and_a_canonical_erasure_covers_retained_material : Specification
{
    static readonly EventStoreName _eventStore = "retained";
    static readonly EventStoreNamespaceName _namespace = "default";
    static readonly EncryptionKeyIdentifier _identifier = "customer-42";
    static readonly EncryptionKey _original = new([1, 2, 3], [4, 5, 6]);
    static readonly EncryptionKey _fresh = new([7, 8, 9], [10, 11, 12]);
    readonly List<(IEncryptionKeyStorage Storage, VersionedSecretClient Client, uint Revision)> _stores = [];
    readonly List<string> _returnedKeys = [];
    readonly List<string> _reportedPresent = [];
    readonly List<string> _unexpectedErrors = [];
    readonly List<bool> _retained = [];

    async Task Establish()
    {
        foreach (var fenced in new[] { false, true })
        {
            foreach (var revision in new[] { 1u, 2u })
            {
                var client = new VersionedSecretClient();
                IEncryptionKeyStorage storage = fenced ? new AzureKeyVaultEncryptionKeyStorage(client, null, new FenceAuthority()) : new AzureKeyVaultEncryptionKeyStorage(client);
                await LegacySecrets.WriteKey(client, LegacySecrets.Name(_eventStore, _identifier, revision.ToString()), _original);
                await client.SetSecretAsync(SecretLocations.For(_eventStore, _namespace, _identifier).ErasureName, JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["erasedThrough"] = "1",
                    ["erasedKeyFingerprints"] = revision == 1 ? "" : _original.Fingerprint,
                    ["newKeyAllowed"] = bool.FalseString
                }));
                _stores.Add((storage, client, revision));
            }
        }
    }

    async Task Because()
    {
        foreach (var (storage, client, revision) in _stores)
        {
            await CheckReads(storage, revision);
            await CheckWrites(storage);
            await storage.AllowNewKeyFor(_eventStore, _namespace, _identifier);
            await CheckReads(storage, revision);
            await CheckErased("authorized save", () => storage.SaveFor(_eventStore, _namespace, _identifier, _original, 1u));
            _retained.Add(await LegacySecrets.IsLive(client, LegacySecrets.Name(_eventStore, _identifier, revision.ToString())));
        }
    }

    async Task CheckReads(IEncryptionKeyStorage storage, uint revision)
    {
        await CheckAbsent("latest pair", storage.TryGetLatestFor(_eventStore, _namespace, _identifier));
        await CheckAbsent("revision pair", storage.TryGetAtRevisionFor(_eventStore, _namespace, _identifier, revision));
        await CheckAbsent("latest revision pair", storage.TryGetAtRevisionFor(_eventStore, _namespace, _identifier, EncryptionKeyRevision.Latest));
        await CheckAbsent("latest key", storage.TryGetFor(_eventStore, _namespace, _identifier));
        await CheckAbsent("revision key", storage.TryGetFor(_eventStore, _namespace, _identifier, revision));
        if (await storage.HasFor(_eventStore, _namespace, _identifier)) _reportedPresent.Add("latest");
        if (await storage.HasFor(_eventStore, _namespace, _identifier, revision)) _reportedPresent.Add("revision");
        await CheckMissing("get latest", () => storage.GetFor(_eventStore, _namespace, _identifier));
        await CheckMissing("get revision", () => storage.GetFor(_eventStore, _namespace, _identifier, revision));
    }

    async Task CheckWrites(IEncryptionKeyStorage storage)
    {
        await CheckErased("provision", () => storage.GetOrAddFor(_eventStore, _namespace, _identifier, _fresh));
        await CheckErased("provision pair", () => storage.GetOrAddWithRevisionFor(_eventStore, _namespace, _identifier, _fresh));
        await CheckErased("save", () => storage.SaveFor(_eventStore, _namespace, _identifier, _original, 1u));
    }

    async Task CheckAbsent<T>(string path, Task<T?> lookup)
        where T : class
    {
        if (await lookup is not null) _returnedKeys.Add(path);
    }

    async Task CheckMissing(string path, Func<Task> lookup)
    {
        if (await Catch.Exception(lookup) is not MissingEncryptionKey) _unexpectedErrors.Add(path);
    }

    async Task CheckErased(string path, Func<Task> lookup)
    {
        if (await Catch.Exception(lookup) is not EncryptionKeyErased) _unexpectedErrors.Add(path);
    }

    [Fact] void should_not_return_covered_material_from_any_lookup() => _returnedKeys.ShouldBeEmpty();
    [Fact] void should_not_report_covered_revisions_as_present() => _reportedPresent.ShouldBeEmpty();
    [Fact] void should_report_missing_or_erased_on_get_and_write_paths() => _unexpectedErrors.ShouldBeEmpty();
    [Fact] void should_leave_the_legacy_material_in_place() => _retained.TrueForAll(_ => _).ShouldBeTrue();
}
