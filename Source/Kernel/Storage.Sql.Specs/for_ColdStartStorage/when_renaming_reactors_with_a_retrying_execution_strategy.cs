// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Storage.Sql.EventStores.Reactors;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_ColdStartStorage;

public class when_renaming_reactors_with_a_retrying_execution_strategy : given.a_cold_start_database
{
    ReactorDefinitionsStorage _storage;
    int _count;
    string _id;

    protected override bool UsesRetryingExecutionStrategy => true;

    async Task Establish()
    {
        _storage = new(Store, _database);
        await _storage.Save(new("original", ReactorOwner.Client, "event-log", [], IsReplayable: true));
    }

    async Task Because()
    {
        await Race(() => _storage.Rename("original", "renamed"), rename: true);
        _count = await _store.Reactors.CountAsync();
        _id = (await _store.Reactors.SingleAsync()).Id;
    }

    [Fact] void should_keep_one_row() => _count.ShouldEqual(1);
    [Fact] void should_keep_the_renamed_reactor() => _id.ShouldEqual("renamed");
}
