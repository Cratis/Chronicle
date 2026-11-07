// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Security;
using Grpc.Core;

namespace Cratis.Chronicle.Services.for_SystemStoreGuard;

public class when_a_validated_administrator_makes_a_request_with_headers : given.a_request_with_forged_credentials
{
    bool _internalAllowed;
    RpcException? _internalRefusal;

    async Task Because()
    {
        await Evaluate(ContextWithForgedHeaders());
        _internalAllowed = _allowed;
        _internalRefusal = _refusal;
        _authentication = new Authentication { Authority = "https://idp.example", ExternalAdministratorClaim = new ExternalAdministratorClaim { Type = "roles", Value = "chronicle-admin" } };
        _caller = new ClaimsPrincipal(new ClaimsIdentity([new Claim("roles", "chronicle-admin")], ExternalAuthority.AuthenticationType));
        await Evaluate(ContextWithForgedHeaders());
    }

    [Fact] void should_allow_the_internal_administrator() => _internalAllowed.ShouldBeTrue();
    [Fact] void should_allow_the_externally_mapped_administrator() => _allowed.ShouldBeTrue();
    [Fact] void should_not_refuse_the_internal_administrator() => _internalRefusal.ShouldBeNull();
    [Fact] void should_not_refuse_the_externally_mapped_administrator() => _refusal.ShouldBeNull();
}
