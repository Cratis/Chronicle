// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using ComplianceKey = Cratis.Chronicle.Storage.Compliance.EncryptionKey;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.for_EncryptionKeyStorage.given;

public abstract class a_key_store(KeyStoreHarness harness) : Specification
{
    protected static readonly string[] _identifiers =
    [
        "Alice",
        "alice",
        "alice ",
        "ALICE  ",
        "Ålesund",
        "ålesund",
        "ａｌｉｃｅ",
        "alice\u0001",
        "alice \u0001"
    ];

    protected readonly KeyStoreHarness _harness = harness;

    public static KeyStoreHarness Sqlite()
    {
        var path = Path.Combine(Path.GetTempPath(), $"chronicle-keys-{Guid.NewGuid():N}.db");
        return new("sqlite", () => Task.FromResult($"DataSource={path};Pooling=False;Default Timeout=30"), _ =>
        {
            File.Delete(path);
            return Task.CompletedTask;
        });
    }

    protected static ComplianceKey KeyFor(int index) => new([(byte)(index + 1)], [(byte)(index + 101)]);

    protected Task<ComplianceKey> Provision(string identifier, int index) =>
        _harness.Storage.GetOrAddFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, identifier, KeyFor(index));

    protected Task<ComplianceKey?> Read(string identifier) =>
        _harness.Storage.TryGetFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, identifier);

    async Task Establish() => await _harness.Initialize();
    async Task Destroy() => await _harness.DisposeAsync();
}
