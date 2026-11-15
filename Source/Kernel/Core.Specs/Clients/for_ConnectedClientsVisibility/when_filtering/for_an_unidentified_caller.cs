// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectedClientsVisibility.when_filtering;

public class for_an_unidentified_caller : given.a_visibility
{
    async Task Because() => _result = await _visibility.Filter(_clients, new(ConnectionOwner.Unidentified, null, null));

    [Fact] void should_see_nothing() => _result.ShouldBeEmpty();
}
