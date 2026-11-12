// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive;

namespace Cratis.Chronicle.EventStores.for_EventStoreNames.when_respecting_store_entitlements;

public class and_the_entitlements_change_during_the_initial_check : given.a_store_list_with_entitlements
{
    void Establish()
    {
        var initial = _cache.Snapshot;
        Grant("second");
        var updated = _cache.Snapshot;
        var firstCheck = true;
        _cache.Snapshot.Returns(_ =>
        {
            if (!firstCheck) return updated;
            firstCheck = false;
            _changes.OnNext(Unit.Default);
            return initial;
        });
    }

    async Task Because() => await Observe();

    [Fact] void should_emit_the_updated_grant_in_the_initial_value() => _observed.ShouldEqual([new EventStoreNames("second", "second")]);
}
