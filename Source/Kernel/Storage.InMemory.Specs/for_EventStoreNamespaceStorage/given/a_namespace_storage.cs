// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.InMemory.for_EventStoreNamespaceStorage.given;

public class a_namespace_storage : Specification
{
    protected EventStoreNamespaceStorage _storage;

    void Establish() => _storage = new(
        new EventStoreName("some-store"),
        new EventStoreNamespaceName("some-namespace"),
        Substitute.For<IJobTypes>(),
        Substitute.For<ISinks>());
}
