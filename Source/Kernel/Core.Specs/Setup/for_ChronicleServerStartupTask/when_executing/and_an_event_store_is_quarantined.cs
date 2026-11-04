// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Naming;

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_an_event_store_is_quarantined : given.a_startup_task
{
    void Establish()
    {
        var snapshot = ResourceNameSnapshot.NotReady();
        snapshot.Publish([_eventStore], []);
        UseResourceNames(snapshot);
    }

    Task Because() => Execute();

    [Fact] void should_not_register_its_event_types() => _eventTypes.DidNotReceive().DiscoverAndRegister(_eventStore);
    [Fact] void should_not_ensure_its_default_namespace() => _namespaces.DidNotReceive().EnsureDefault();
    [Fact] void should_not_list_its_namespaces() => _namespaces.DidNotReceive().GetAll();
    [Fact] void should_not_ensure_its_projections() => _projectionsManager.DidNotReceive().Ensure();
    [Fact] void should_not_open_its_storage() => _storage.DidNotReceive().GetEventStore(_eventStore);
}
