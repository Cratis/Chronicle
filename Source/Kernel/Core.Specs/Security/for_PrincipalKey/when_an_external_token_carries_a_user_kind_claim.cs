// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_PrincipalKey;

public class when_an_external_token_carries_a_user_kind_claim : given.principals
{
    bool _externalIsLocalUser;
    bool _localIsLocalUser;

    void Because()
    {
        _externalIsLocalUser = PrincipalKey.IsLocalUser(Principal(ExternalIssuer, "someone", (ChronicleClaimTypes.Principal, ChronicleClaimTypes.User)), _external);
        _localIsLocalUser = PrincipalKey.IsLocalUser(Principal(System.Security.Claims.ClaimsIdentity.DefaultIssuer, "someone", (ChronicleClaimTypes.Principal, ChronicleClaimTypes.User)), _internal);
    }

    [Fact] void should_not_make_the_external_principal_a_local_user() => _externalIsLocalUser.ShouldBeFalse();
    [Fact] void should_recognize_a_local_user() => _localIsLocalUser.ShouldBeTrue();
}
