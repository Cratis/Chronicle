// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_OAuthTokenProvider.when_requesting_a_token;

public class and_the_external_endpoint_is_not_https : Specification
{
    CapturingTokenEndpointHandler _handler;
    Exception _httpResult;
    Exception _userInfoResult;

    void Establish() => _handler = new CapturingTokenEndpointHandler();

    void Because()
    {
        _httpResult = Catch.Exception(() => Create("http://idp.example.com/token"));
        _userInfoResult = Catch.Exception(() => Create("https://user:pw@idp.example.com/token"));
    }

    [Fact] void should_reject_http() => _httpResult.ShouldBeOfExactType<InsecureTokenEndpoint>();
    [Fact] void should_reject_user_info() => _userInfoResult.ShouldBeOfExactType<InsecureTokenEndpoint>();
    [Fact] void should_not_leak_the_user_info() => _userInfoResult.Message.Contains("pw", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_name_the_rejected_endpoint() => new[] { _httpResult, _userInfoResult }.All(_ => !_.Message.Contains("idp.example.com", StringComparison.Ordinal) && !_.Message.Contains("/token", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_post_anything() => _handler.Requests.ShouldBeEmpty();

    OAuthTokenProvider Create(string endpoint) => new(
        new Uri(endpoint),
        "test-client",
        "test-secret",
        null,
        Substitute.For<ILogger<OAuthTokenProvider>>(),
        _handler,
        new ControllableTimeProvider());
}
