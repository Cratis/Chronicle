// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Security;
using Grpc.Core;

namespace Cratis.Chronicle.Services.for_SystemStoreGuard;

public class when_a_request_forges_an_administrator_with_headers : given.a_request_with_forged_credentials
{
    void Establish() => _caller = new ClaimsPrincipal(new ClaimsIdentity(
        [new Claim("sub", _administratorId.ToString()), new Claim(ChronicleClaimTypes.Principal, ChronicleClaimTypes.User)]));

    async Task Because() => await Evaluate(ContextWithForgedHeaders());

    [Fact] void should_not_allow_the_caller() => _allowed.ShouldBeFalse();
    [Fact] void should_refuse_with_permission_denied() => _refusal!.StatusCode.ShouldEqual(StatusCode.PermissionDenied);
}
