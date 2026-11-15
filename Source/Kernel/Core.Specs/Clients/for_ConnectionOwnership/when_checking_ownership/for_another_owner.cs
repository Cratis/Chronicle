// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionOwnership.when_checking_ownership;

public class for_another_owner : given.a_connection_ownership
{
    bool _result;

    void Establish() => _ownership.Claim(Id, Alice);

    void Because() => _result = _ownership.IsOwnedBy(Id, Bob);

    [Fact] void should_not_be_owned() => _result.ShouldBeFalse();
}
