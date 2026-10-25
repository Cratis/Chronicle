// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Security.for_AdministratorsReactor;

public class when_a_removed_users_grant_arrives_late : given.an_administrators_reactor
{
    EventContext _grantContext;
    EventContext _removalContext;
    IEnumerable<Administrator> _privileges;
    IEnumerable<Administrator> _beforeRemoval;

    async Task Establish()
    {
        _grantContext = SeedGrant();
        await _reactor.Granted(_grant, _grantContext);
        _beforeRemoval = await _storage.System.Administrators.GetAll();
        _removalContext = Seed(_userId, new UserRemoved());
    }

    async Task Because()
    {
        await _reactor.Removed(new UserRemoved(), _removalContext);
        await _reactor.Granted(_grant, _grantContext);
        await _reactor.Granted(_grant, _grantContext);
        _privileges = await _storage.System.Administrators.GetAll();
    }

    [Fact] void should_start_with_a_real_privilege() => _beforeRemoval.Single().Id.ShouldEqual(_userId);
    [Fact] void should_not_resurrect_the_removed_users_privilege() => _privileges.ShouldBeEmpty();
}
