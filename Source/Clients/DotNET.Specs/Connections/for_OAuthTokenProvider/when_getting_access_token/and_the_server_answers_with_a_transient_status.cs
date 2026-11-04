// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_OAuthTokenProvider.when_getting_access_token;

public class and_the_server_answers_with_a_transient_status : given.a_token_provider_with_controllable_endpoint
{
    Exception _error;

    void Establish() => _endpoint.RespondWithStatus = HttpStatusCode.ServiceUnavailable;

    async Task Because() => _error = await Catch.Exception(async () => await _provider.GetAccessToken());

    [Fact] void should_fail() => _error.ShouldBeOfExactType<HttpRequestException>();
    [Fact] void should_log_a_warning() => _logger.AtLevel(LogLevel.Warning).Count().ShouldEqual(1);
    [Fact] void should_not_log_an_error() => _logger.AtLevel(LogLevel.Error).ShouldBeEmpty();
}
