// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Storage.Sql.EventStores.Reactors;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_ColdStartStorage;

public class when_renaming_reactors_onto_an_existing_reactor : given.a_cold_start_database
{
    ReactorDefinitionsStorage _storage;
    Exception _exception;
    bool _originalReplayable;
    bool _targetReplayable;

    async Task Establish()
    {
        _storage = new(Store, _database);
        await _storage.Save(new("original", ReactorOwner.Client, "event-log", [], IsReplayable: true));
        await _storage.Save(new("target", ReactorOwner.Client, "event-log", [], IsReplayable: false));
    }

    async Task Because()
    {
        _exception = await Catch.Exception(() => _storage.Rename("original", "target"));
        _originalReplayable = (await _store.Reactors.AsNoTracking().SingleAsync(reactor => reactor.Id == "original")).IsReplayable;
        _targetReplayable = (await _store.Reactors.AsNoTracking().SingleAsync(reactor => reactor.Id == "target")).IsReplayable;
    }

    [Fact] void should_fail() => _exception.ShouldBeOfExactType<DbUpdateException>();
    [Fact] void should_keep_the_original_unchanged() => _originalReplayable.ShouldBeTrue();
    [Fact] void should_keep_the_target_unchanged() => _targetReplayable.ShouldBeFalse();
}
