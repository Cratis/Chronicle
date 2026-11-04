// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameAudit;

public class when_an_inventory_keeps_failing : given.an_audit
{
    Exception _result;

    void Establish() =>
        _inventory.Read(Arg.Any<CancellationToken>()).Returns(_ => Task.FromException<IEnumerable<ResourceUse>>(new InvalidOperationException()));

    async Task Because() => _result = await Catch.Exception(() => Run());

    [Fact] void should_throw_the_failure() => _result.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_be_ready() => _snapshot.Check("orders").ShouldEqual(ResourceNameStatus.NotReady);
}
