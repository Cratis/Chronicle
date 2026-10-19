// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_round_tripping;

public class without_type_set_control : given.an_observer_state_storage
{
    KernelObserverState _saved;
    KernelObserverState _missing;
    string _storedControl;

    async Task Because()
    {
        await _storage.Save(new() { Identifier = "legacy-observer" });
        _saved = await _storage.Get("legacy-observer");
        _missing = await _storage.Get("missing-observer");
        await using var context = CreateContext();
        _storedControl = await context.Observers.Where(observer => observer.Id == "legacy-observer").Select(observer => observer.TypeSetControl).SingleAsync();
    }

    [Fact] void should_keep_the_legacy_observer_uninitialized() => _saved.TypeSetControl.ShouldBeNull();
    [Fact] void should_keep_the_missing_observer_uninitialized() => _missing.TypeSetControl.ShouldBeNull();
    [Fact] void should_store_sql_null_not_a_new_control_document() => _storedControl.ShouldBeNull();
}
