// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionOwnership.when_releasing;

public class the_last_stream : given.a_connection_ownership
{
    void Establish() => _ownership.Claim(Id, Alice);

    void Because() => _ownership.Release(Id, Alice);

    [Fact] void should_no_longer_be_owned() => _ownership.IsOwnedBy(Id, Alice).ShouldBeFalse();
}
