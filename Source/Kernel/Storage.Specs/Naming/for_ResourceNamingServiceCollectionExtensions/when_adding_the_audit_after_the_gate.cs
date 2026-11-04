// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNamingServiceCollectionExtensions;

public class when_adding_the_audit_after_the_gate : Specification
{
    IServiceProvider _afterTheGate;
    IServiceProvider _beforeTheGate;

    void Because()
    {
        _afterTheGate = new ServiceCollection().AddResourceNameGate().AddResourceNameAudit().BuildServiceProvider();
        _beforeTheGate = new ServiceCollection().AddResourceNameAudit().AddResourceNameGate().BuildServiceProvider();
    }

    [Fact] void should_not_be_ready_when_the_gate_was_added_first() => _afterTheGate.GetRequiredService<IResourceNameSnapshot>().Check("orders").ShouldEqual(ResourceNameStatus.NotReady);
    [Fact] void should_use_one_snapshot_when_the_gate_was_added_first() => _afterTheGate.GetRequiredService<IResourceNameSnapshot>().ShouldEqual(_afterTheGate.GetRequiredService<ResourceNameSnapshot>());
    [Fact] void should_not_be_ready_when_the_audit_was_added_first() => _beforeTheGate.GetRequiredService<IResourceNameSnapshot>().Check("orders").ShouldEqual(ResourceNameStatus.NotReady);
    [Fact] void should_use_one_snapshot_when_the_audit_was_added_first() => _beforeTheGate.GetRequiredService<IResourceNameSnapshot>().ShouldEqual(_beforeTheGate.GetRequiredService<ResourceNameSnapshot>());
}
