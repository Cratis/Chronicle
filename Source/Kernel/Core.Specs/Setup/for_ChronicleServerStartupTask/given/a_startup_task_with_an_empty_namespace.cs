// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.given;

public class a_startup_task_with_an_empty_namespace : a_startup_task
{
    protected EventStoreNamespaceName _emptyNamespace = "empty-namespace";

    void Establish()
    {
        var emptyStorage = Substitute.For<IEventStoreNamespaceStorage>();
        emptyStorage.HasData().Returns(false);
        _eventStoreStorage.GetNamespace(_emptyNamespace).Returns(emptyStorage);
        _namespaces.GetAll().Returns([_emptyNamespace, _namespace]);
    }
}
