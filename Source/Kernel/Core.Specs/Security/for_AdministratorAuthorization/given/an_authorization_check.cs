// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Security.for_AdministratorAuthorization.given;

public class an_authorization_check : Specification
{
    protected UserId _userId;
    protected IAdministratorPrivileges _privileges;
    protected ClaimsPrincipal _principal;
    protected AdministratorAuthorization _authorization;
    protected bool _authorized;

    protected AdministratorAuthorization CreateAuthorization(Authentication authentication) => new(
        Options.Create(new ChronicleOptions { Authentication = authentication }), _privileges, NullLogger<AdministratorAuthorization>.Instance);

    void Establish()
    {
        _userId = Guid.NewGuid();
        _privileges = Substitute.For<IAdministratorPrivileges>();
        _privileges.IsActiveAdministrator(_userId).Returns(true);
        _principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", _userId.ToString()), new Claim(ChronicleClaimTypes.Principal, ChronicleClaimTypes.User)], "spec"));
        _authorization = CreateAuthorization(new Authentication());
    }
}
