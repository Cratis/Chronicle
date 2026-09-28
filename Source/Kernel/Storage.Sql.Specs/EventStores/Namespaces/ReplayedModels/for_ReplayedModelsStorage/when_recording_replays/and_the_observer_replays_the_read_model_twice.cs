// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReplayedModels.for_ReplayedModelsStorage.when_recording_replays;

public class and_the_observer_replays_the_read_model_twice : given.a_migrated_namespace_database
{
    static readonly DateTimeOffset _firstStarted = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset _secondStarted = new(2026, 1, 2, 12, 0, 0, TimeSpan.Zero);

    ReplayedModelsStorage _storage;
    ReadModelOccurrence _first;
    ReadModelOccurrence _second;
    ReadModelOccurrence[] _occurrences;

    void Establish()
    {
        _storage = new ReplayedModelsStorage(_eventStore, _namespace, _database);
        var readModel = new ReadModelType(new ReadModelIdentifier("some-read-model"), new ReadModelGeneration(2));
        _first = new("some-observer", _firstStarted, readModel, "some-read-model", "some-read-model-20260101120000");
        _second = new("some-observer", _secondStarted, readModel, "some-read-model", "some-read-model-20260102120000");
    }

    async Task Because()
    {
        await _storage.Replayed(_first);
        await _storage.Replayed(_second);
        _occurrences = (await _storage.GetOccurrences(_first.Type.Identifier)).OrderBy(_ => _.Occurred).ToArray();
    }

    [Fact] void should_keep_both_replays() => _occurrences.ShouldContainOnly(_first, _second);
}
