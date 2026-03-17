// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Namespaces;

namespace Cratis.Chronicle.Setup.for_ChronicleInitializer.given;

public class a_chronicle_initializer : all_dependencies
{
    internal ChronicleInitializer _initializer;
    protected INamespaces _systemNamespacesGrain;

    void Establish()
    {
        _systemNamespacesGrain = Substitute.For<INamespaces>();
        _grainFactory.GetGrain<INamespaces>(EventStoreName.System.Value).Returns(_systemNamespacesGrain);
        _systemNamespacesGrain.GetAll()
            .Returns(Task.FromResult<IEnumerable<EventStoreNamespaceName>>(
                [EventStoreNamespaceName.Default]));
        _storage.GetEventStores()
            .Returns(Task.FromResult<IEnumerable<EventStoreName>>([]));

        _initializer = new ChronicleInitializer(
            _storage,
            _eventTypes,
            _reactors,
            _projectionsServiceClient,
            _grainFactory,
            _authenticationService);
    }
}
