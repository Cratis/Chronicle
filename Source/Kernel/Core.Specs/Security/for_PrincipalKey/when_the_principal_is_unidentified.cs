// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Security.for_PrincipalKey;

public class when_the_principal_is_unidentified : given.principals
{
    PrincipalKey _key;

    void Because() => _key = PrincipalKey.From(new ClaimsPrincipal(new ClaimsIdentity()), _internal);

    [Fact] void should_be_unidentified() => _key.IsUnidentified.ShouldBeTrue();
    [Fact] void should_use_the_anonymous_actor() => PrincipalKey.Actor(null, _internal).ShouldEqual("anonymous");
}
