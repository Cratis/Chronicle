// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Security;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Cratis.Chronicle.Server.Authentication.for_ServiceCollectionExtensions;

public class when_an_external_token_carries_the_mapped_role : given.an_external_token_policy_check
{
    async Task Because() => _allowed = await Authorize("reader", "chronicle-admin");

    [Fact] void should_authorize() => _allowed.ShouldBeTrue();
    [Fact] void should_match_the_token_handler_short_claim_type_property() => ExternalAuthority.ShortClaimTypeProperty.ShouldEqual(JsonWebTokenHandler.ShortClaimTypeProperty);
}
