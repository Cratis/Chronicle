// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using VaultSharp;
using VaultSharp.Core;
using VaultSharp.V1.Commons;
using VaultSharp.V1.SecretsEngines;

namespace Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_path;

public class and_a_canonical_erasure_covers_retained_material : Specification
{
    static readonly EventStoreName _eventStore = "retained";
    static readonly EventStoreNamespaceName _namespace = "default";
    static readonly EncryptionKeyIdentifier _identifier = "customer-42";
    static readonly EncryptionKey _original = new([1, 2, 3], [4, 5, 6]);
    static readonly EncryptionKey _fresh = new([7, 8, 9], [10, 11, 12]);
    readonly List<(IEncryptionKeyStorage Storage, Dictionary<string, IDictionary<string, object>> Secrets, uint Revision)> _stores = [];
    readonly List<string> _returnedKeys = [];
    readonly List<string> _reportedPresent = [];
    readonly List<string> _unexpectedErrors = [];
    readonly List<bool> _retained = [];

    void Establish()
    {
        foreach (var revision in new[] { 1u, 2u })
        {
            var locations = VaultKeyLocations.For(_eventStore, _namespace, _identifier);
            var secrets = new Dictionary<string, IDictionary<string, object>>
            {
                [locations.LegacyPath(revision)!] = new Dictionary<string, object>
                {
                    ["publicKey"] = Convert.ToBase64String(_original.Public),
                    ["privateKey"] = Convert.ToBase64String(_original.Private)
                },
                [locations.ErasurePath] = new Dictionary<string, object>
                {
                    ["erasedThrough"] = "1",
                    ["erasedKeyFingerprints"] = revision == 1 ? "" : _original.Fingerprint,
                    ["newKeyAllowed"] = bool.FalseString
                },
                [VaultPathSegments.LayoutMarker] = new Dictionary<string, object> { ["format"] = VaultPathSegments.Root }
            };
            _stores.Add((new VaultEncryptionKeyStorage(Client(secrets)), secrets, revision));
        }
    }

    async Task Because()
    {
        foreach (var (storage, secrets, revision) in _stores)
        {
            await CheckReads(storage, revision);
            await CheckWrites(storage);
            await storage.AllowNewKeyFor(_eventStore, _namespace, _identifier);
            await CheckReads(storage, revision);
            await CheckErased("authorized save", () => storage.SaveFor(_eventStore, _namespace, _identifier, _original, 1u));
            _retained.Add(secrets.ContainsKey(VaultKeyLocations.For(_eventStore, _namespace, _identifier).LegacyPath(revision)!));
        }
    }

    static IVaultClient Client(Dictionary<string, IDictionary<string, object>> secrets)
    {
        var client = Substitute.For<IVaultClient>();
        var engine = client.V1.Secrets.KeyValue.V2;
        client.V1.System.GetSecretBackendAsync("secret").Returns(new Secret<SecretsEngine>
        {
            Data = new SecretsEngine { Type = SecretsEngineType.KeyValueV1, Options = new Dictionary<string, object> { ["version"] = "2" } }
        });
        engine.ReadSecretAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(call => Task.FromResult(new Secret<SecretData>
            {
                Data = new SecretData
                {
                    Data = secrets.TryGetValue(call.ArgAt<string>(0), out var data) ? data : throw new VaultApiException(HttpStatusCode.NotFound, "Missing"),
                    Metadata = new CurrentSecretMetadata { Version = 1 }
                }
            }));
        engine.ReadSecretPathsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(call => Task.FromResult(new Secret<ListInfo>
            {
                Data = new ListInfo { Keys = secrets.Keys.Where(_ => _.StartsWith($"{call.ArgAt<string>(0)}/", StringComparison.Ordinal)).Select(_ => _[(call.ArgAt<string>(0).Length + 1)..]).ToList() }
            }));
        engine.WriteSecretAsync(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>(), Arg.Any<int?>(), Arg.Any<string>())
            .Returns(call =>
            {
                secrets[call.ArgAt<string>(0)] = call.ArgAt<IDictionary<string, object>>(1);
                return Task.FromResult(new Secret<CurrentSecretMetadata> { Data = new CurrentSecretMetadata { Version = 1 } });
            });
        return client;
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
