// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using StoredAdministrator = Cratis.Chronicle.Storage.Security.Administrator;

namespace Cratis.Chronicle.Security.for_AdministratorsReactor;

public class when_another_stream_contains_an_unsigned_roster_revocation : given.an_administrators_reactor
{
    EventContext _context;
    IEnumerable<StoredAdministrator> _administrators;

    void Establish()
    {
        _context = SeedGrant();
        SeedWithoutProof(_context.EventSourceId, new UserRevokedAdministrator(_userId, "spec"), "other");
    }

    async Task Because()
    {
        await _reactor.Granted(_grant, _context);
        _administrators = await _storage.System.Administrators.GetAll();
    }

    [Fact] void should_not_project_privilege() => _administrators.ShouldBeEmpty();
}
