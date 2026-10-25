// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using StoredAdministrator = Cratis.Chronicle.Storage.Security.Administrator;

namespace Cratis.Chronicle.Security.for_AdministratorsReactor;

public class when_a_user_removal_is_replayed : given.an_administrators_reactor
{
    IEnumerable<StoredAdministrator> _privileges;
    IEnumerable<StoredAdministrator> _beforeRemoval;

    async Task Because()
    {
        await _reactor.Granted(_grant, SeedGrant());
        _beforeRemoval = await _storage.System.Administrators.GetAll();
        var context = Seed(_userId, new UserRemoved());
        await _reactor.Removed(new UserRemoved(), context);
        await _reactor.Removed(new UserRemoved(), context);
        _privileges = await _storage.System.Administrators.GetAll();
    }

    [Fact] void should_start_with_the_granted_privilege() => _beforeRemoval.Single().Id.ShouldEqual(_userId);
    [Fact] void should_remove_the_privilege_without_an_explicit_revocation_event() => _privileges.ShouldBeEmpty();
}
