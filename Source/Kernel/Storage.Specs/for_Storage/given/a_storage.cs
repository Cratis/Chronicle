// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.for_Storage.given;

public class a_storage : Specification
{
    protected ControllableClusterStorage _clusterStorage;
    protected Storage _storage;

    void Establish()
    {
        _clusterStorage = new ControllableClusterStorage();
        for (var index = 0; index < 4; index++)
        {
            _clusterStorage.Provide(Substitute.For<IEventStoreStorage>());
        }

        _storage = new Storage(_clusterStorage, Substitute.For<ISystemStorage>(), new KnownInstancesOf<ISinkFactory>([]));
    }

    void Destroy() => _clusterStorage.Dispose();
}
