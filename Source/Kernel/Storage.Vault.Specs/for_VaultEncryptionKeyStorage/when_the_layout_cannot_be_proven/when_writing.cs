// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreUpgradeCheck.when_the_layout_cannot_be_proven;
using VaultSharp.V1.SecretsEngines.KeyValue.V2;

namespace Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_the_layout_cannot_be_proven;

public class when_writing : Specification
{
    static readonly EncryptionKey _key = new([1, 2, 3], [4, 5, 6]);

    List<string> _wronglyAccepted = [];
    List<string> _wronglyWritten = [];

    async Task Because()
    {
        foreach (var step in new[] { UnprovableLayouts.Step.ReadMarker, UnprovableLayouts.Step.ListRoot, UnprovableLayouts.Step.WriteMarker, UnprovableLayouts.Step.ReadMarkerBack })
        {
            foreach (var (name, create) in UnprovableLayouts.Failures)
            {
                var (client, engine) = UnprovableLayouts.Create(step, create());
                var storage = new VaultEncryptionKeyStorage(client);
                var writes = new (string Name, Func<Task> Write)[]
                {
                    ("save", () => storage.SaveFor("store", "ns", "id", _key)),
                    ("provision", () => storage.GetOrAddFor("store", "ns", "id", _key)),
                    ("erase", () => storage.RecordErasureFor("store", "ns", "id"))
                };
                foreach (var (writeName, write) in writes)
                {
                    var error = await Catch.Exception(write);
                    if (error is not EncryptionKeyLayoutUnusable)
                    {
                        _wronglyAccepted.Add($"{step}/{name}/{writeName}");
                    }
                }

                var keyWrites = engine.ReceivedCalls()
                    .Count(call => call.GetMethodInfo().Name == nameof(IKeyValueSecretsEngineV2.WriteSecretAsync) && call.GetArguments()[0] is string path && path.StartsWith("k1/", StringComparison.Ordinal));
                if (keyWrites > 0)
                {
                    _wronglyWritten.Add($"{step}/{name}");
                }
            }
        }
    }

    [Fact] void should_refuse_every_write() => _wronglyAccepted.ShouldBeEmpty();
    [Fact] void should_not_write_under_the_root() => _wronglyWritten.ShouldBeEmpty();
}
