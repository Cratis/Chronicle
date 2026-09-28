// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReplayedModels.for_ReplayedModelsStorage.when_recording_replays;

/// <summary>
/// A grain state write that failed after recording a replay records it again when it is retried.
/// </summary>
public class and_the_same_replay_is_recorded_again : given.a_migrated_namespace_database
{
    static readonly DateTimeOffset _started = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero).AddTicks(1_234_567);

    ReplayedModelsStorage _storage;
    ReadModelOccurrence _occurrence;
    Exception _error;
    ReadModelOccurrence[] _occurrences;

    async Task Establish()
    {
        _storage = new ReplayedModelsStorage(_eventStore, _namespace, _database);
        var readModel = new ReadModelType(new ReadModelIdentifier("some-read-model"), ReadModelGeneration.First);
        _occurrence = new("some-observer", _started, readModel, "some-read-model", "some-read-model-20260101120000");
        await _storage.Replayed(_occurrence);
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _storage.Replayed(_occurrence));
        _occurrences = (await _storage.GetOccurrences(_occurrence.Type.Identifier)).ToArray();
    }

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_keep_one_record_of_the_replay() => _occurrences.Length.ShouldEqual(1);
}
