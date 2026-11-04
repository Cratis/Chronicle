// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Naming;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.InMemory.for_InMemoryResourceInventory;

public class when_reading : Specification
{
    EventStoreStorages _storages;
    ResourceUse[] _uses;
    int _countBefore;

    async Task Establish()
    {
        _storages = new(new KnownInstancesOf<ISinkFactory>([]), Substitute.For<Cratis.Orleans.Storage.IJobsStorage>());
        var storage = _storages.GetOrCreate("orders");
        await storage.Namespaces.Ensure("acme");
        _countBefore = _storages.Names.Count();
    }

    async Task Because() => _uses = [.. await new InMemoryResourceInventory(_storages).Read(CancellationToken.None)];

    void Destroy() => _storages.Dispose();

    [Fact] void should_report_the_store() => _uses.Any(_ => _.Owner?.EventStore == "orders" && _.Owner.Namespace is null).ShouldBeTrue();
    [Fact] void should_report_the_namespace() => _uses.Any(_ => _.Owner?.EventStore == "orders" && _.Owner.Namespace == "acme").ShouldBeTrue();
    [Fact] void should_report_the_system_store() => _uses.Any(_ => _.Owner?.EventStore == "System" && _.Owner.Namespace is null).ShouldBeTrue();
    [Fact] void should_not_create_event_stores() => _storages.Names.Count().ShouldEqual(_countBefore);
}
