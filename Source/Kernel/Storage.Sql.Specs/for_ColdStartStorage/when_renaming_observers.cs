// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_ColdStartStorage;

public class when_renaming_observers : given.a_cold_start_database
{
    ObserverStateStorage _storage;
    int _count;
    string _id;

    async Task Establish()
    {
        _storage = new(Store, Namespace, _database);
        await _storage.Save(new() { Identifier = "original", NextEventSequenceNumber = new(42) }, Concepts.EventSequences.EventSequenceId.Log);
    }

    async Task Because()
    {
        await Race(() => _storage.Rename("original", "renamed"), rename: true);
        _count = await _namespace.Observers.CountAsync();
        _id = (await _namespace.Observers.SingleAsync()).Id.Value;
    }

    [Fact] void should_keep_one_row() => _count.ShouldEqual(1);
    [Fact] void should_keep_the_renamed_observer() => _id.ShouldEqual("renamed");
}
