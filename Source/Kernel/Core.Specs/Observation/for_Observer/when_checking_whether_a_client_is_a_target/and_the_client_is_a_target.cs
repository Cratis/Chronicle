// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_checking_whether_a_client_is_a_target;

public class and_the_client_is_a_target : given.an_observer_with_a_superseded_client
{
    bool _result;

    async Task Because() => _result = await _observer.IsClientTarget(_activeClient.ConnectionId);

    [Fact] void should_be_a_target() => _result.ShouldBeTrue();
}
