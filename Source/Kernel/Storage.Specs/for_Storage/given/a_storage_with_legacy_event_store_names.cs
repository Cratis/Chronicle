// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.for_Storage.given;

public class a_storage_with_legacy_event_store_names : Specification
{
    protected Storage _storage;
    protected IClusterStorage _clusterStorage;
    protected Subject<IEnumerable<EventStoreName>> _changes;

    void Establish()
    {
        _clusterStorage = Substitute.For<IClusterStorage>();
        _clusterStorage.GetEventStores().Returns(Task.FromResult<IEnumerable<EventStoreName>>(LegacyNames()));
        _changes = new();
        _clusterStorage.ObserveEventStores().Returns(_changes);
        _storage = new(_clusterStorage, Substitute.For<ISystemStorage>(), Substitute.For<IInstancesOf<ISinkFactory>>());
    }

    protected static EventStoreName[] LegacyNames() =>
        [null!, "", " ", EventStoreName.NotSet, "store-a", "store-b"];

    void Destroy() => _changes.Dispose();
}
