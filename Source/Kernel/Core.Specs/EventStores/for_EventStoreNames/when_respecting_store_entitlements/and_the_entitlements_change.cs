// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive;

namespace Cratis.Chronicle.EventStores.for_EventStoreNames.when_respecting_store_entitlements;

public class and_the_entitlements_change : given.a_store_list_with_entitlements
{
    async Task Because()
    {
        await ReadLists();
        Grant("second");
        _changes.OnNext(Unit.Default);
        await ReadEmission();
    }

    [Fact] void should_have_initially_listed_the_first_store() => _snapshot.ShouldEqual([new EventStoreNames("first", "first")]);
    [Fact] void should_remove_the_old_grant_and_include_the_new_grant() => _observed.ShouldEqual([new EventStoreNames("second", "second")]);
}
