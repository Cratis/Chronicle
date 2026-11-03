// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_OAuthTokenProvider.when_getting_access_token;

public class and_the_transport_is_unavailable : given.a_token_provider_with_controllable_endpoint
{
    Exception _error;

    void Establish() => _endpoint.Fail = true;

    async Task Because() => _error = await Catch.Exception(async () => await _provider.GetAccessToken());

    [Fact] void should_fail_with_the_transport_error() => _error.ShouldBeOfExactType<HttpRequestException>();
    [Fact] void should_not_log_an_error() => _logger.AtLevel(LogLevel.Error).ShouldBeEmpty();
    [Fact] void should_not_log_a_warning() => _logger.AtLevel(LogLevel.Warning).ShouldBeEmpty();
    [Fact] void should_keep_the_details_at_debug() => _logger.AtLevel(LogLevel.Debug).Any(_ => _.Exception is not null).ShouldBeTrue();
}
