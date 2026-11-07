// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionOwnership.when_checking_ownership;

public class for_an_unknown_id : given.a_connection_ownership
{
    bool _result;

    void Because() => _result = _ownership.IsOwnedBy("unknown", Alice);

    [Fact] void should_not_be_owned() => _result.ShouldBeFalse();
}
