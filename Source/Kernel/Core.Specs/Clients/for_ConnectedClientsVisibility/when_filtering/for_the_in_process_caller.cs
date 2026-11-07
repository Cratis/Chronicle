// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectedClientsVisibility.when_filtering;

public class for_the_in_process_caller : given.a_visibility
{
    async Task Because() => _result = await _visibility.Filter(_clients, ConnectionCaller.InProcess);

    [Fact] void should_see_every_connection() => _result.Count().ShouldEqual(2);
}
