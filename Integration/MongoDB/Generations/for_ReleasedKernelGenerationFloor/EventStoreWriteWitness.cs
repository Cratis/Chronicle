// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.MongoDB.Integration.Generations.for_ReleasedKernelGenerationFloor;

/// <summary>
/// Independent mutation oracle: records every write the MongoDB server executes against event-store databases, using the
/// database profiler rather than any kernel code.
/// </summary>
/// <param name="client">The host-side MongoDB client.</param>
public sealed class EventStoreWriteWitness(IMongoClient client)
{
    static readonly string[] _systemDatabases = ["admin", "local", "config"];
    static readonly string[] _writeCommands = ["insert", "update", "delete", "findAndModify", "create", "createIndexes", "drop", "dropIndexes", "dropDatabase", "renameCollection"];

    HashSet<string> _armed = [];

    /// <summary>Starts profiling every write on every existing database and remembers which databases exist.</summary>
    /// <returns>The asynchronous operation.</returns>
    public async Task Arm()
    {
        _armed = [.. await DatabaseNames()];
        foreach (var name in _armed)
        {
            var database = client.GetDatabase(name);
            await database.RunCommandAsync<BsonDocument>(new BsonDocument { { "profile", 0 } });
            await database.DropCollectionAsync("system.profile");
            await database.RunCommandAsync<BsonDocument>(new BsonDocument { { "profile", 2 }, { "slowms", 0 } });
        }
    }

    /// <summary>Gets the event-store databases (store and namespace) that appeared after arming.</summary>
    /// <returns>The database names.</returns>
    public async Task<IReadOnlyList<string>> CreatedEventStoreDatabases() =>
        (await DatabaseNames()).Where(name => !_armed.Contains(name) && IsEventStoreDatabase(name)).ToArray();

    /// <summary>Counts the writes executed against event-store databases (store and namespace) since arming.</summary>
    /// <returns>The number of profiled writes.</returns>
    public async Task<long> EventStoreWrites()
    {
        long count = 0;
        foreach (var name in _armed.Where(IsEventStoreDatabase))
        {
            var profile = client.GetDatabase(name).GetCollection<BsonDocument>("system.profile");
            var writes = Builders<BsonDocument>.Filter.In("op", ["insert", "update", "remove"]) |
                (Builders<BsonDocument>.Filter.Eq("op", "command") & Builders<BsonDocument>.Filter.Or(_writeCommands.Select(command => Builders<BsonDocument>.Filter.Exists($"command.{command}"))));
            count += await profile.CountDocumentsAsync(writes & Builders<BsonDocument>.Filter.Ne("ns", $"{name}.system.profile"));
        }
        return count + (await CreatedEventStoreDatabases()).Count;
    }

    static bool IsEventStoreDatabase(string name) => name.Contains("+es", StringComparison.Ordinal);

    async Task<List<string>> DatabaseNames()
    {
        using var names = await client.ListDatabaseNamesAsync();
        return (await names.ToListAsync()).Where(name => !_systemDatabases.Contains(name)).ToList();
    }
}
