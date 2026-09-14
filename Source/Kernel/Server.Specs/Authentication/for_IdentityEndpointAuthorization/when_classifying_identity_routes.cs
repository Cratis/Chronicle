// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication.for_IdentityEndpointAuthorization;

public class when_classifying_identity_routes : Specification
{
    [Fact] void should_allow_login() => IdentityEndpointAuthorization.IsAnonymousRoute("/identity/login").ShouldBeTrue();
    [Fact] void should_allow_refresh() => IdentityEndpointAuthorization.IsAnonymousRoute("/identity/refresh").ShouldBeTrue();
    [Fact] void should_protect_registration() => IdentityEndpointAuthorization.IsAnonymousRoute("/identity/register").ShouldBeFalse();
    [Fact] void should_protect_user_management() => IdentityEndpointAuthorization.IsAnonymousRoute("/identity/manage/info").ShouldBeFalse();
    [Fact] void should_protect_two_factor_management() => IdentityEndpointAuthorization.IsAnonymousRoute("/identity/manage/2fa").ShouldBeFalse();
}
