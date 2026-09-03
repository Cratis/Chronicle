// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using OpenIddict.Abstractions;

namespace Cratis.Chronicle.Server.Authentication.OpenIddict.for_ChronicleTokenClaimsExtensions;

public class when_setting_the_chronicle_audience : Specification
{
    ClaimsIdentity _identity;

    void Establish() => _identity = new ClaimsIdentity();

    void Because() => _identity.SetChronicleAudience();

    [Fact] void should_set_only_the_chronicle_audience() => _identity.GetAudiences().ShouldContainOnly([WellKnownAudiences.Chronicle]);
}
