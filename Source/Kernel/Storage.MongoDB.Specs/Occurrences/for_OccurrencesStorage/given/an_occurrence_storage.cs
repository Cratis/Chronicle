// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.Occurrences;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Occurrences.for_OccurrencesStorage.given;

public abstract class an_occurrence_storage(ReplicaSetMongoDBFixture fixture) : Specification
{
    protected readonly TriggerId _trigger = new(Guid.NewGuid().ToString("N"));
    protected readonly DateTimeOffset _now = DateTimeOffset.UnixEpoch.AddDays(1);
    protected ScheduleToken _token;
    protected IOccurrencesStorage _storage;
    protected IScheduleStorage _schedules;
    protected Occurrence _occurrence;
    IMongoClient _client;
    IMongoDatabase _database;
    string _databaseName;

    protected IOccurrencesStorage Recreate() => new OccurrencesStorage(_database);
    protected IScheduleStorage RecreateSchedules() => new OccurrencesStorage(_database);

    void Establish()
    {
        _client = new MongoClient(fixture.ConnectionString);
        _databaseName = MongoDBSpecDatabaseNames.New();
        _database = _client.GetDatabase(_databaseName);
        var concrete = new OccurrencesStorage(_database);
        _storage = concrete;
        _schedules = concrete;
        _token = new ScheduleToken(_trigger.Value);
        _occurrence = new(Guid.NewGuid().ToString("N"), _trigger, OccurrenceKind.Application, "first", "{\"value\":1}", 1, _now, null, null, OccurrenceState.Pending, 0, 0, null, null, null, null);
    }

    async Task Destroy() => await _client.DropDatabaseAsync(_databaseName);
}
