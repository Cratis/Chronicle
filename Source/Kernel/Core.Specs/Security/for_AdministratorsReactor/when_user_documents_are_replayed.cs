// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Security;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Security.for_AdministratorsReactor;

public class when_user_documents_are_replayed : Specification
{
    IStorage _storage;
    AdministratorsReactor _administrators;
    UsersReactor _users;
    EventContext _context;
    UserId _userId;
    Administrator[] _privileges;

    void Establish()
    {
        _userId = Guid.NewGuid();
        _context = EventContext.Empty with { EventSourceId = _userId };
        _storage = Substitute.For<IStorage>();
        _storage.System.Returns(new Storage.InMemory.SystemStorage());
        _administrators = new AdministratorsReactor(_storage);
        _users = new UsersReactor(_storage.System.Users, NullLogger<UsersReactor>.Instance);
    }

    async Task Because()
    {
        var granted = new UserGrantedAdministrator(_userId, GrantReason.Bootstrap, "spec");
        await _administrators.Granted(granted, _context);
        await _administrators.Granted(granted, _context);
        await _users.Added(new UserAdded("ordinary", "user@example.com", "hash"), _context);
        var stale = await _storage.System.Users.GetById(_userId);
        await _storage.System.Users.Update(stale!);
        await _users.Added(new UserAdded("ordinary", "user@example.com", "hash"), _context);
        _privileges = (await _storage.System.Administrators.GetAll()).ToArray();
    }

    [Fact] void should_preserve_the_separate_privilege() => _privileges.Single().Id.ShouldEqual(_userId);
    [Fact] void should_converge_replayed_grants() => _privileges.Length.ShouldEqual(1);
}
