// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverStorageCompatibility;

public class when_checking_the_downgrade_floor : ObserverStorageCompatibilityContract
{
    EventStoreStorages _stores;

    void Establish() => _stores = new(new KnownInstancesOf<ISinkFactory>([]), Substitute.For<Orleans.Storage.IJobsStorage>());

    protected override IEventStoreStorage OpenStore() => _stores.GetOrCreate("orders");
    protected override IObserverStateStorage OpenObservers() => OpenStore().GetNamespace("default").Observers;

    void Destroy() => _stores.Dispose();
}
