// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Server.Authentication.OpenIddict;
using OpenIddict.Abstractions;

namespace Cratis.Chronicle.Server.Authentication.for_ServiceCollectionExtensions;

public class when_validating_a_data_protection_token_for_chronicle : given.data_protection_access_tokens
{
    string _token;
    ClaimsPrincipal _result;

    async Task Establish() => _token = await CreateToken(WellKnownAudiences.Chronicle);

    async Task Because() => _result = await _validation.ValidateAccessTokenAsync(_token);

    [Fact] void should_issue_a_data_protection_token_rather_than_a_jwt() => _token.StartsWith("CfDJ8", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_preserve_the_authenticated_subject() => _result.GetClaim(OpenIddictConstants.Claims.Subject).ShouldEqual(Subject);
    [Fact] void should_preserve_the_chronicle_audience() => _result.GetAudiences().ShouldContainOnly(WellKnownAudiences.Chronicle);
    [Fact] void should_use_the_shared_persisted_key_ring() => _sharedKeys.Count.ShouldEqual(1);
    [Fact] void should_allow_an_independent_provider_to_read_the_shared_key() => _validator.XmlRepository.GetAllElements().Count.ShouldEqual(1);
}
