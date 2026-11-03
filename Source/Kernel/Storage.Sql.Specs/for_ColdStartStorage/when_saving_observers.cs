// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_ColdStartStorage;

public class when_saving_observers : given.a_cold_start_database
{
    int _count;
    ulong _progress;

    async Task Because()
    {
        var storage = new ObserverStateStorage(Store, Namespace, _database);
        await Race(() => storage.Save(new() { Identifier = "observer", NextEventSequenceNumber = new(42) }, Concepts.EventSequences.EventSequenceId.Log));
        _count = await _namespace.Observers.CountAsync();
        _progress = (await _namespace.Observers.SingleAsync()).NextEventSequenceNumber;
    }

    [Fact] void should_keep_one_row() => _count.ShouldEqual(1);
    [Fact] void should_keep_the_progress() => _progress.ShouldEqual(42UL);
}
