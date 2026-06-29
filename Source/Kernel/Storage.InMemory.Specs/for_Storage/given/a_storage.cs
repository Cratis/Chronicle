// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.InMemory.for_Storage.given;

public class a_storage : Specification
{
    protected EventStoreStorages _eventStoreStorages;
    protected Storage _storage;

    void Establish()
    {
        _eventStoreStorages = new(new KnownInstancesOf<ISinkFactory>([]), Substitute.For<IJobTypes>());
        _storage = new(_eventStoreStorages, Substitute.For<ISystemStorage>());
    }

    void Destroy() => _eventStoreStorages.Dispose();
}
