// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Security.for_AdministratorsReactor;

public class when_removal_is_appended_during_a_projection_write : given.an_administrators_reactor
{
    IAdministratorStorage _actual;
    EventContext _context;
    IEnumerable<Administrator> _privileges;
    int _writes;

    void Establish()
    {
        _context = SeedGrant();
        _actual = _storage.System.Administrators;
        var administrators = Substitute.For<IAdministratorStorage>();
        administrators.Save(Arg.Any<Administrator>()).Returns(call =>
        {
            _writes++;
            Seed(_userId, new UserRemoved());
            return _actual.Save(call.Arg<Administrator>());
        });
        administrators.Delete(Arg.Any<UserId>()).Returns(call => _actual.Delete(call.Arg<UserId>()));
        var system = Substitute.For<ISystemStorage>();
        system.Administrators.Returns(administrators);
        _storage.System.Returns(system);
    }

    async Task Because()
    {
        await _reactor.Granted(_grant, _context);
        _privileges = await _actual.GetAll();
    }

    [Fact] void should_exercise_a_stale_projection_write() => _writes.ShouldEqual(1);
    [Fact] void should_reconcile_again_instead_of_leaving_a_resurrected_privilege() => _privileges.ShouldBeEmpty();
}
