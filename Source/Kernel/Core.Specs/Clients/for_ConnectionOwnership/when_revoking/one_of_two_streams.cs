// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionOwnership.when_revoking;

public class one_of_two_streams : given.a_connection_ownership
{
    void Establish()
    {
        _ownership.Claim(Id, Alice);
        _ownership.Claim(Id, Alice);
    }

    void Because() => _ownership.Revoke(Id, Alice);

    [Fact] void should_keep_the_owner_in_place_for_the_other_stream() => _ownership.IsOwnedBy(Id, Alice).ShouldBeTrue();
}
