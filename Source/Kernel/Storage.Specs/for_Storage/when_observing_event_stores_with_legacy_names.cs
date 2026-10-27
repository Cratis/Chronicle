// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.for_Storage;

public class when_observing_event_stores_with_legacy_names : given.a_storage_with_legacy_event_store_names
{
    IEnumerable<EventStoreName> _result;
    IDisposable _subscription;

    void Because()
    {
        _subscription = _storage.ObserveEventStores().Subscribe(names => _result = names);
        _changes.OnNext(LegacyNames());
    }

    [Fact] void should_only_publish_valid_names() => _result.ShouldEqual(new EventStoreName[] { "store-a", "store-b" });

    void Destroy() => _subscription.Dispose();
}
