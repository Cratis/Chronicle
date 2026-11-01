// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using StoredAdministrator = Cratis.Chronicle.Storage.Security.Administrator;

namespace Cratis.Chronicle.Security.for_AdministratorsReactor;

public class when_the_system_store_name_has_different_casing : given.an_administrators_reactor
{
    IEnumerable<StoredAdministrator> _privileges;

    async Task Because()
    {
        await _reactor.Granted(_grant, SeedGrant() with { EventStore = "sYsTeM" });
        _privileges = await _storage.System.Administrators.GetAll();
    }

    [Fact] void should_accept_the_system_event() => _privileges.Single().Id.ShouldEqual(_userId);
}
