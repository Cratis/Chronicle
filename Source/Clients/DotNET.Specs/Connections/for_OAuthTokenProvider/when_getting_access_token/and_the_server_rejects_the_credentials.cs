// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_OAuthTokenProvider.when_getting_access_token;

public class and_the_server_rejects_the_credentials : given.a_token_provider_with_controllable_endpoint
{
    Exception _error;

    void Establish() => _endpoint.RespondWithStatus = HttpStatusCode.Unauthorized;

    async Task Because() => _error = await Catch.Exception(async () => await _provider.GetAccessToken());

    [Fact] void should_fail() => _error.ShouldBeOfExactType<HttpRequestException>();
    [Fact] void should_log_one_error() => _logger.AtLevel(LogLevel.Error).Count().ShouldEqual(1);
    [Fact] void should_not_attach_a_stack_trace_to_the_error() => _logger.AtLevel(LogLevel.Error).All(_ => _.Exception is null).ShouldBeTrue();
    [Fact] void should_not_log_the_client_secret() => _logger.Entries.Any(_ => _.Message.Contains("test-secret")).ShouldBeFalse();
    [Fact] void should_not_log_the_token() => _logger.Entries.Any(_ => _.Message.Contains(_endpoint.Token)).ShouldBeFalse();
}
