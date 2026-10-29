// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Arc.Authorization;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_granting_through_the_management_command : given.an_administrator_roster
{
    readonly string _actor = Guid.NewGuid().ToString();
    ICurrentPrincipalAccessor _principalAccessor;
    UserGrantedAdministrator _grant;
    bool _administrator;

    async Task Establish()
    {
        await AddUser(_first, "new-administrator");
        _grainFactory.GetGrain<IAdministratorRoster>(Concepts.Security.AdministratorRoster.Key).Returns(_roster);
        _principalAccessor = Substitute.For<ICurrentPrincipalAccessor>();
        _principalAccessor.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", _actor)], "spec")));
    }

    async Task Because()
    {
        await new GrantAdministrator(_first).Handle(_grainFactory, _principalAccessor);
        _grant = Recorded<UserGrantedAdministrator>().Single();
        _administrator = await _roster.IsActiveAdministrator(_first);
    }

    [Fact] void should_grant_through_the_authoritative_roster() => _administrator.ShouldBeTrue();
    [Fact] void should_record_the_management_grant_reason() => _grant.Reason.ShouldEqual(GrantReason.AdministratorGrant);
    [Fact]
    void should_audit_the_actor_without_copying_its_token()
    {
        using var evidence = JsonDocument.Parse(_grant.Evidence);
        evidence.RootElement.GetProperty("Actor").GetString().ShouldEqual(_actor);
    }
}
