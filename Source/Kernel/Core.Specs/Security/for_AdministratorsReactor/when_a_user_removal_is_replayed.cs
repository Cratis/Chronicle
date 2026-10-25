// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Security.for_AdministratorsReactor;

public class when_a_user_removal_is_replayed : Specification
{
    IStorage _storage;
    AdministratorsReactor _reactor;
    EventContext _context;
    UserId _userId;
    IEnumerable<Administrator> _privileges;

    void Establish()
    {
        _userId = Guid.NewGuid();
        _context = EventContext.Empty with { EventSourceId = _userId };
        _storage = Substitute.For<IStorage>();
        _storage.System.Returns(new Storage.InMemory.SystemStorage());
        _reactor = new AdministratorsReactor(_storage);
    }

    async Task Because()
    {
        await _reactor.Granted(new UserGrantedAdministrator(_userId, GrantReason.Bootstrap, "spec"), _context);
        await _reactor.Removed(new UserRemoved(), _context);
        await _reactor.Removed(new UserRemoved(), _context);
        _privileges = await _storage.System.Administrators.GetAll();
    }

    [Fact] void should_remove_the_privilege_without_an_explicit_revocation_event() => _privileges.ShouldBeEmpty();
}
