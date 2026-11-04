// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameAudit;

public class when_an_inventory_fails_transiently_then_succeeds : given.an_audit
{
    void Establish() =>
        _inventory.Read(Arg.Any<CancellationToken>()).Returns(
            _ => Task.FromException<IEnumerable<ResourceUse>>(new TimeoutException()),
            _ => Task.FromResult<IEnumerable<ResourceUse>>([Use("orders", "db:orders")]));

    async Task Because() => await Run(isTransient: _ => true);

    [Fact] void should_read_twice() => _inventory.Received(2).Read(Arg.Any<CancellationToken>());
    [Fact] void should_be_ready() => _snapshot.Check("orders").ShouldEqual(ResourceNameStatus.Available);
}
