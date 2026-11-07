// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Configuration;
using Grpc.Core;

namespace Cratis.Chronicle.Services.for_SystemStoreGuard;

public class when_a_request_forges_an_external_administrator_with_headers : given.a_request_with_forged_credentials
{
    void Establish()
    {
        _authentication = new Authentication { Authority = "https://idp.example", ExternalAdministratorClaim = new ExternalAdministratorClaim { Type = "roles", Value = "chronicle-admin" } };
        _caller = new ClaimsPrincipal(new ClaimsIdentity([new Claim("roles", "chronicle-admin")]));
    }

    async Task Because() => await Evaluate(ContextWithForgedHeaders());

    [Fact] void should_not_allow_the_caller() => _allowed.ShouldBeFalse();
    [Fact] void should_refuse_with_permission_denied() => _refusal!.StatusCode.ShouldEqual(StatusCode.PermissionDenied);
}
