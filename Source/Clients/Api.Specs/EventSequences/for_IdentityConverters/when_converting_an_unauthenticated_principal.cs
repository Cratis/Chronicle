// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Api.EventSequences.for_IdentityConverters;

public class when_converting_an_unauthenticated_principal : Specification
{
    Contracts.Identities.Identity _result;

    void Because() => _result = new ClaimsPrincipal(new ClaimsIdentity()).ToContract();

    [Fact] void should_use_the_anonymous_subject() => _result.Subject.ShouldEqual("anonymous");
}
