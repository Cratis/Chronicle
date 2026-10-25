// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.Extensions.Logging.Abstractions;
using StoredAdministrator = Cratis.Chronicle.Storage.Security.Administrator;

namespace Cratis.Chronicle.Security.for_AdministratorsReactor;

public class when_user_documents_are_replayed : given.an_administrators_reactor
{
    UsersReactor _users;
    EventContext _context;
    EventContext _grantContext;
    StoredAdministrator[] _privileges;

    void Establish()
    {
        _grantContext = SeedGrant();
        _context = _grantContext with { EventSourceId = _userId };
        _users = new UsersReactor(_storage.System.Users, NullLogger<UsersReactor>.Instance);
    }

    async Task Because()
    {
        await _reactor.Granted(_grant, _grantContext);
        await _reactor.Granted(_grant, _grantContext);
        await _users.Added(new UserAdded("ordinary", "user@example.com", "hash"), _context);
        var stale = await _storage.System.Users.GetById(_userId);
        await _storage.System.Users.Update(stale!);
        await _users.Added(new UserAdded("ordinary", "user@example.com", "hash"), _context);
        _privileges = (await _storage.System.Administrators.GetAll()).ToArray();
    }

    [Fact] void should_preserve_the_separate_privilege() => _privileges.Single().Id.ShouldEqual(_userId);
    [Fact] void should_converge_replayed_grants() => _privileges.Length.ShouldEqual(1);
}
