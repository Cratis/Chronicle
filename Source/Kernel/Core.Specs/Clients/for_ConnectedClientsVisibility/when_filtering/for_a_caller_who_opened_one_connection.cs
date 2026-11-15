// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectedClientsVisibility.when_filtering;

public class for_a_caller_who_opened_one_connection : given.a_visibility
{
    async Task Because() => _result = await _visibility.Filter(_clients, CallerFor(Alice));

    [Fact] void should_see_only_its_own_connection() => _result.Select(_ => _.ConnectionId).ShouldContainOnly("alice-connection");
}
