// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectedClientsVisibility.when_filtering;

public class with_authentication_disabled : given.a_visibility
{
    protected override bool AuthenticationEnabled => false;

    async Task Because() => _result = await _visibility.Filter(_clients, ConnectionCaller.Anonymous);

    [Fact] void should_see_every_connection() => _result.Count().ShouldEqual(2);
}
