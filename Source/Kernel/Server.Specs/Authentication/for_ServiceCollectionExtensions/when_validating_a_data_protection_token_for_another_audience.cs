// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Server.Authentication.OpenIddict;
using OpenIddict.Abstractions;

namespace Cratis.Chronicle.Server.Authentication.for_ServiceCollectionExtensions;

public class when_validating_a_data_protection_token_for_another_audience : given.data_protection_access_tokens
{
    string _token;
    Exception _exception;

    async Task Establish()
    {
        var control = await CreateToken(WellKnownAudiences.Chronicle);
        await _validation.ValidateAccessTokenAsync(control);
        _token = await CreateToken("another-resource");
    }

    async Task Because() => _exception = await Catch.Exception(async () => await _validation.ValidateAccessTokenAsync(_token));

    [Fact] void should_reject_the_otherwise_valid_token() => _exception.ShouldBeOfExactType<OpenIddictExceptions.ProtocolException>();
    [Fact] void should_report_an_invalid_token() => ((OpenIddictExceptions.ProtocolException)_exception).Error.ShouldEqual(OpenIddictConstants.Errors.InvalidToken);
}
