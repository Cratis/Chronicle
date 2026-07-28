// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.InMemory.for_EventStoreStorages.given;

public class an_empty_registry : Specification
{
    protected EventStoreStorages _storages;

    void Establish() => _storages = new(new KnownInstancesOf<ISinkFactory>([]), Substitute.For<IJobTypes>());

    void Destroy() => _storages.Dispose();
}
