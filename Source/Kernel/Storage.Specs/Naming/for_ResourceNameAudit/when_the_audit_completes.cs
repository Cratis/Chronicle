// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameAudit;

public class when_the_audit_completes : given.an_audit
{
    void Establish() => Returns(
        Use("orders", "db:orders"),
        Use("orders", "db:orders-main", "main"),
        Use("first", "db:shared"),
        Use("second", "db:shared"));

    async Task Because() => await Run();

    [Fact] void should_be_ready() => _snapshot.Check("orders").ShouldEqual(ResourceNameStatus.Available);
    [Fact] void should_quarantine_the_overlapping_stores() => _snapshot.Check("first").ShouldEqual(ResourceNameStatus.Quarantined);
    [Fact] void should_register_the_store() => _gate.Received(1).MarkRegistered(new EventStoreName("orders"));
    [Fact] void should_register_the_namespace() => _gate.Received(1).MarkRegistered(new EventStoreName("orders"), new EventStoreNamespaceName("main"));
    [Fact] void should_not_register_a_quarantined_store() => _gate.DidNotReceive().MarkRegistered(new EventStoreName("first"));
}
