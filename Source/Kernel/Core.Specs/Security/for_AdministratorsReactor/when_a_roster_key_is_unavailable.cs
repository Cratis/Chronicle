// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.AspNetCore.DataProtection;
using StoredAdministrator = Cratis.Chronicle.Storage.Security.Administrator;

namespace Cratis.Chronicle.Security.for_AdministratorsReactor;

public class when_a_roster_key_is_unavailable : given.an_administrators_reactor
{
    EventContext _context;
    IEnumerable<StoredAdministrator> _administrators;

    void Establish()
    {
        _context = SeedGrant();
        Seed(_context.EventSourceId, new UserRevokedAdministrator(_userId, "spec"), signer: new KernelEventProof(new EphemeralDataProtectionProvider()));
    }

    async Task Because()
    {
        await _reactor.Granted(_grant, _context);
        _administrators = await _storage.System.Administrators.GetAll();
    }

    [Fact] void should_not_project_privilege() => _administrators.ShouldBeEmpty();
}
