// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameAudit;

public class when_refusing_collisions : given.an_audit
{
    Exception _result;

    void Establish() => Returns(Use("first", "db:shared"), Use("second", "db:shared"));

    async Task Because() => _result = await Catch.Exception(() => Run(NameCollisionHandling.Refuse));

    [Fact] void should_throw_that_collisions_were_found() => _result.ShouldBeOfExactType<ResourceNameCollisionsFound>();
    [Fact] void should_not_be_ready() => _snapshot.Check("first").ShouldEqual(ResourceNameStatus.NotReady);
}
