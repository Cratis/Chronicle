// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_OAuthTokenProvider.when_getting_access_token;

public class and_the_request_is_cancelled : given.a_token_provider_with_controllable_endpoint
{
    Exception _error;

    void Establish() => _endpoint.Cancel = true;

    async Task Because() => _error = await Catch.Exception(async () => await _provider.GetAccessToken());

    [Fact] void should_throw_operation_cancelled() => _error.ShouldBeAssignableFrom<OperationCanceledException>();
    [Fact] void should_not_log_at_warning_or_above() => _logger.Entries.Where(_ => _.Level >= LogLevel.Warning).ShouldBeEmpty();
}
