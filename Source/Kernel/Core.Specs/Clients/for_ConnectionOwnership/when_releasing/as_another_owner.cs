// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionOwnership.when_releasing;

public class as_another_owner : given.a_connection_ownership
{
    void Establish() => _ownership.Claim(Id, Alice);

    void Because() => _ownership.Release(Id, Bob);

    [Fact] void should_leave_the_owner_in_place() => _ownership.IsOwnedBy(Id, Alice).ShouldBeTrue();
}
