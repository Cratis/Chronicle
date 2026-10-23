// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_ColdStartStorage;

public class when_renaming_observers_onto_an_existing_observer : given.a_cold_start_database
{
    ObserverStateStorage _storage;
    Exception _exception;
    ulong _originalNext;
    ulong _targetNext;

    async Task Establish()
    {
        _storage = new(Store, Namespace, _database);
        await _storage.Save(new() { Identifier = "original", NextEventSequenceNumber = new(42) });
        await _storage.Save(new() { Identifier = "target", NextEventSequenceNumber = new(7) });
    }

    async Task Because()
    {
        _exception = await Catch.Exception(() => _storage.Rename("original", "target"));
        _originalNext = (await _namespace.Observers.AsNoTracking().SingleAsync(observer => observer.Id == "original")).NextEventSequenceNumber;
        _targetNext = (await _namespace.Observers.AsNoTracking().SingleAsync(observer => observer.Id == "target")).NextEventSequenceNumber;
    }

    [Fact] void should_fail() => _exception.ShouldBeOfExactType<DbUpdateException>();
    [Fact] void should_keep_the_original_unchanged() => _originalNext.ShouldEqual(42UL);
    [Fact] void should_keep_the_target_unchanged() => _targetNext.ShouldEqual(7UL);
}
