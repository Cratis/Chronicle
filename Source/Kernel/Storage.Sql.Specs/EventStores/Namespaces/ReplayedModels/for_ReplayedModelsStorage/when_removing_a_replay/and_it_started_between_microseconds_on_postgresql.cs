// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReplayedModels.for_ReplayedModelsStorage.when_removing_a_replay;

/// <summary>
/// Retention removes the occurrence the replay manager holds in memory, with every tick of its start, while
/// PostgreSQL only stored whole microseconds of it.
/// </summary>
/// <param name="fixture">The <see cref="PostgreSqlFixture"/> supplying the container.</param>
[Collection(PostgreSqlCollection.Name)]
public class and_it_started_between_microseconds_on_postgresql(PostgreSqlFixture fixture) : given.a_migrated_postgresql_namespace_database(fixture)
{
    static readonly DateTimeOffset _firstStarted = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero).AddTicks(1_234_567);
    static readonly DateTimeOffset _secondStarted = new DateTimeOffset(2026, 1, 2, 12, 0, 0, TimeSpan.Zero).AddTicks(7_654_321);

    ReplayedModelsStorage _storage;
    ReadModelOccurrence _first;
    ReadModelOccurrence _second;
    ReadModelOccurrence[] _remaining;

    async Task Establish()
    {
        _storage = new ReplayedModelsStorage(_eventStore, _namespace, _database);
        var readModel = new ReadModelType(new ReadModelIdentifier("some-read-model"), ReadModelGeneration.First);
        _first = new("some-observer", _firstStarted, readModel, "some-read-model", "some-read-model-20260101120000");
        _second = new("some-observer", _secondStarted, readModel, "some-read-model", "some-read-model-20260102120000");
        await _storage.Replayed(_first);
        await _storage.Replayed(_second);
    }

    async Task Because()
    {
        await _storage.Remove(_first);
        _remaining = (await _storage.GetOccurrences(_first.Type.Identifier)).ToArray();
    }

    [Fact] void should_keep_only_the_other_replay() => _remaining.Length.ShouldEqual(1);
    [Fact] void should_keep_the_other_replays_backup() => _remaining[0].RevertContainerName.ShouldEqual(_second.RevertContainerName);
}
