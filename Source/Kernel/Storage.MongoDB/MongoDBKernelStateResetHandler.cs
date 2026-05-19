// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Drops all Chronicle-owned databases when the kernel is reset between integration test specs.
/// The container stays running; only the data is wiped. The kernel's bootstrap handler then
/// re-creates the system event store, namespaces, and default identity data.
/// </summary>
/// <param name="chronicleOptions"><see cref="ChronicleOptions"/> describing the active storage backend.</param>
/// <param name="mongoDBOptions"><see cref="MongoDBOptions"/> with the connection details.</param>
/// <param name="clientManager"><see cref="IMongoDBClientManager"/> used to obtain a client.</param>
public class MongoDBKernelStateResetHandler(
    IOptions<ChronicleOptions> chronicleOptions,
    IOptions<MongoDBOptions> mongoDBOptions,
    IMongoDBClientManager clientManager) : ICanPerformKernelStateReset
{
    static readonly HashSet<string> _preservedDatabases = new(StringComparer.OrdinalIgnoreCase)
    {
        // MongoDB system databases
        "admin",
        "config",
        "local",

        // Chronicle cluster database — holds users, applications, data protection keys,
        // applied patches, and system information. Wiping these would invalidate the
        // client's cached JWT (signing keys rotate) and force every test class boundary
        // to re-bootstrap auth from scratch. The bootstrap handler can recreate them,
        // but in-flight client tokens issued before the reset would still be rejected.
        WellKnownDatabaseNames.Chronicle,
    };

    /// <inheritdoc/>
    public bool CanReset()
    {
        var type = chronicleOptions.Value.Storage.Type;
        return string.IsNullOrEmpty(type)
            || (!string.Equals(type, StorageType.Sqlite, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(type, StorageType.MsSql, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(type, StorageType.PostgreSql, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc/>
    public async Task Reset()
    {
        var url = new MongoUrl(mongoDBOptions.Value.Server);
        var settings = MongoClientSettings.FromUrl(url);
        settings.DirectConnection = mongoDBOptions.Value.DirectConnection;
        var client = clientManager.GetClientFor(settings);

        using var cursor = await client.ListDatabaseNamesAsync();
        var names = await cursor.ToListAsync();
        foreach (var name in names.Where(n => !_preservedDatabases.Contains(n)))
        {
            await client.DropDatabaseAsync(name);
        }
    }
}
