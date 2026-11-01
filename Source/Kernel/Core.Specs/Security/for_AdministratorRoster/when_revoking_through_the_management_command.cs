// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Arc.Authorization;

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_revoking_through_the_management_command : given.an_administrator_roster
{
    ICurrentPrincipalAccessor _principalAccessor;
    UserRevokedAdministrator _revocation;
    bool _administrator;

    async Task Establish()
    {
        await AddUser(_first, "first");
        await AddUser(_second, "second");
        SeedGrant(_first);
        SeedGrant(_second);
        _grainFactory.GetGrain<IAdministratorRoster>(Concepts.Security.AdministratorRoster.Key).Returns(_roster);
        _principalAccessor = Substitute.For<ICurrentPrincipalAccessor>();
        _principalAccessor.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", _second.ToString())], "spec")));
    }

    async Task Because()
    {
        await new RevokeAdministrator(_first, "no longer on call").Handle(_grainFactory, _principalAccessor);
        _revocation = Recorded<UserRevokedAdministrator>().Single();
        _administrator = await _roster.IsActiveAdministrator(_first);
    }

    [Fact] void should_remove_the_authoritative_privilege() => _administrator.ShouldBeFalse();
    [Fact]
    void should_audit_the_actor()
    {
        using var evidence = JsonDocument.Parse(_revocation.RevokeReason);
        evidence.RootElement.GetProperty("Actor").GetString().ShouldEqual(_second.ToString());
    }

    [Fact]
    void should_audit_the_supplied_reason()
    {
        using var evidence = JsonDocument.Parse(_revocation.RevokeReason);
        evidence.RootElement.GetProperty("Reason").GetString().ShouldEqual("no longer on call");
    }
}
