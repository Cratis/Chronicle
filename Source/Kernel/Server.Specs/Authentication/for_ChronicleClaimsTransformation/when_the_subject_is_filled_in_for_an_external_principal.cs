// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Clients;

namespace Cratis.Chronicle.Server.Authentication.for_ChronicleClaimsTransformation;

public class when_the_subject_is_filled_in_for_an_external_principal : given.a_claims_transformation
{
    ConnectionOwner _externalOwner;
    ConnectionOwner _localOwner;

    async Task Because()
    {
        var external = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "u1", ClaimValueTypes.String, "https://external")], "Bearer"));
        var local = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "u1")], "Identity.Application"));
        _result = await _transformation.TransformAsync(external);
        var localResult = await _transformation.TransformAsync(local);
        _externalOwner = ConnectionOwner.From(_result);
        _localOwner = ConnectionOwner.From(localResult);
    }

    [Fact] void should_keep_the_issuer_of_the_source_claim() => _result.FindFirst("sub")!.Issuer.ShouldEqual("https://external");
    [Fact] void should_keep_the_original_issuer_of_the_source_claim() => _result.FindFirst("sub")!.OriginalIssuer.ShouldEqual("https://external");
    [Fact] void should_be_owned_by_the_external_issuer() => _externalOwner.Issuer.ShouldEqual("https://external");
    [Fact] void should_not_share_an_owner_with_a_local_user_with_the_same_id() => _externalOwner.Equals(_localOwner).ShouldBeFalse();
}
