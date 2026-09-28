// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReplayedModels.for_ReplayedModelsStorage;

public class when_removing_a_replay : given.a_migrated_namespace_database
{
    static readonly DateTimeOffset _firstStarted = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset _secondStarted = new(2026, 1, 2, 12, 0, 0, TimeSpan.Zero);

    ReplayedModelsStorage _storage;
    ReadModelOccurrence _first;
    ReadModelOccurrence _second;
    IEnumerable<ReadModelOccurrence> _remaining;

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
        _remaining = await _storage.GetOccurrences(_first.Type.Identifier);
    }

    [Fact] void should_keep_only_the_other_replay() => _remaining.ShouldContainOnly(_second);
}
