// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Security;
using StoredAdministrator = Cratis.Chronicle.Storage.Security.Administrator;

namespace Cratis.Chronicle.Security.for_AdministratorsReactor;

public class when_other_streams_contain_privilege_events : given.an_administrators_reactor
{
    EventContext _context;
    IEnumerable<StoredAdministrator> _privileges;

    void Establish()
    {
        _context = SeedGrant();
        Seed(_context.EventSourceId, new UserRevokedAdministrator(_userId, "another stream"), streamId: "other");
        Seed(_userId, new UserRemoved(), streamType: "other");
        Seed(_userId, new UserGrantedAdministrator(_userId, GrantReason.OperatorRecovery, "wrong source"));
    }

    async Task Because()
    {
        await _reactor.Granted(_grant, _context);
        _privileges = await _storage.System.Administrators.GetAll();
    }

    [Fact] void should_reduce_only_the_authoritative_roster_and_user_streams() => _privileges.Single().ShouldEqual(new StoredAdministrator(_userId, _grant.Reason, _grant.Evidence));
}
