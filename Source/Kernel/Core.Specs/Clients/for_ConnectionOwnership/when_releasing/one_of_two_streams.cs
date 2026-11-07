// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionOwnership.when_releasing;

public class one_of_two_streams : given.a_connection_ownership
{
    void Establish()
    {
        _ownership.Claim(Id, Alice);
        _ownership.Claim(Id, Alice);
    }

    void Because() => _ownership.Release(Id, Alice);

    [Fact] void should_still_be_owned() => _ownership.IsOwnedBy(Id, Alice).ShouldBeTrue();
}
