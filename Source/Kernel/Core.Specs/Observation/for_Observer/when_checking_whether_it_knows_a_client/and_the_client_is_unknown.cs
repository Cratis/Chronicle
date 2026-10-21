// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_checking_whether_it_knows_a_client;

public class and_the_client_is_unknown : given.an_observer_with_a_superseded_client
{
    bool _result;

    async Task Because() => _result = await _observer.KnowsClient("an-unknown-connection");

    [Fact] void should_not_know_it() => _result.ShouldBeFalse();
}
