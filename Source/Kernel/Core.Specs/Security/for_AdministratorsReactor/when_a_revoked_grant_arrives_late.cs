// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using StoredAdministrator = Cratis.Chronicle.Storage.Security.Administrator;

namespace Cratis.Chronicle.Security.for_AdministratorsReactor;

public class when_a_revoked_grant_arrives_late : given.an_administrators_reactor
{
    EventContext _grantContext;
    EventContext _revocationContext;
    UserRevokedAdministrator _revoked;
    IEnumerable<StoredAdministrator> _privileges;
    IEnumerable<StoredAdministrator> _beforeRevocation;

    async Task Establish()
    {
        _grantContext = SeedGrant();
        await _reactor.Granted(_grant, _grantContext);
        _beforeRevocation = await _storage.System.Administrators.GetAll();
        _revoked = new UserRevokedAdministrator(_userId, "operator revocation");
        _revocationContext = Seed(_grantContext.EventSourceId, _revoked);
    }

    async Task Because()
    {
        await _reactor.Revoked(_revoked, _revocationContext);
        await _reactor.Granted(_grant, _grantContext);
        await _reactor.Granted(_grant, _grantContext);
        _privileges = await _storage.System.Administrators.GetAll();
    }

    [Fact] void should_start_with_a_real_privilege() => _beforeRevocation.Single().Id.ShouldEqual(_userId);
    [Fact] void should_not_resurrect_the_revoked_privilege() => _privileges.ShouldBeEmpty();
}
