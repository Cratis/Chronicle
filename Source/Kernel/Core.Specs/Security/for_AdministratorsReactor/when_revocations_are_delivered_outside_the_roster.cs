// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using StoredAdministrator = Cratis.Chronicle.Storage.Security.Administrator;

namespace Cratis.Chronicle.Security.for_AdministratorsReactor;

public class when_revocations_are_delivered_outside_the_roster : given.an_administrators_reactor
{
    EventContext _context;
    IEnumerable<StoredAdministrator> _privileges;

    async Task Establish()
    {
        _context = SeedGrant();
        await _storage.System.Administrators.Save(new StoredAdministrator(_userId, _grant.Reason, _grant.Evidence));
    }

    async Task Because()
    {
        var revoked = new UserRevokedAdministrator(_userId, "forged");
        await _reactor.Revoked(revoked, _context with { EventStore = "tenant-store" });
        await _reactor.Revoked(revoked, _context with { Namespace = "tenant" });
        await _reactor.Revoked(revoked, _context with { EventSourceId = _userId });
        await _reactor.Revoked(revoked, _context with { EventStreamId = "another-stream" });
        await _reactor.Removed(new UserRemoved(), _context with { EventStore = "tenant-store", EventSourceId = _userId });
        await _reactor.Removed(new UserRemoved(), _context with { Namespace = "tenant", EventSourceId = _userId });
        await _reactor.Removed(new UserRemoved(), _context with { EventStreamType = "another-stream-type", EventSourceId = _userId });
        await _reactor.Removed(new UserRemoved(), _context);
        _privileges = await _storage.System.Administrators.GetAll();
    }

    [Fact] void should_preserve_the_privilege() => _privileges.Single().Id.ShouldEqual(_userId);
    [Fact] void should_not_read_the_authoritative_roster() => _events.ReceivedCalls().ShouldBeEmpty();
}
