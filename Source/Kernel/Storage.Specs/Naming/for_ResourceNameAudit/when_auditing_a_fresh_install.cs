// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameAudit;

public class when_auditing_a_fresh_install : given.an_audit
{
    void Establish() => Returns(
        Use("System", "db:system"),
        new ResourceUse(null, ResourceKind.Kernel, "db:cluster"));

    async Task Because() => await Run(NameCollisionHandling.Refuse);

    [Fact] void should_be_ready() => _snapshot.Check("orders").ShouldEqual(ResourceNameStatus.Available);
    [Fact] void should_not_quarantine_the_system_event_store() => _snapshot.Check("System").ShouldEqual(ResourceNameStatus.Available);
}
