// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReplayedModels;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_ColdStartStorage;

public class when_recording_replays : given.a_cold_start_database
{
    int _count;
    string _saved;

    async Task Because()
    {
        var storage = new ReplayedModelsStorage(Store, Namespace, _database);
        var occurrence = new ReadModelOccurrence("observer", DateTimeOffset.Parse("2026-01-01T12:00:00Z"), new ReadModelType("model", ReadModelGeneration.First), "model", "revert");
        await Race(() => storage.Replayed(occurrence));
        _count = await _namespace.ReplayedModels.CountAsync();
        _saved = (await _namespace.ReplayedModels.SingleAsync()).RevertModelName;
    }

    [Fact] void should_keep_one_row() => _count.ShouldEqual(1);
    [Fact] void should_keep_the_replay_data() => _saved.ShouldEqual("revert");
}
