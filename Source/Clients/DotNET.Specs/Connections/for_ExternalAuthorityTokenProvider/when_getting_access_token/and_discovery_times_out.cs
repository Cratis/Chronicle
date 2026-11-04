// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_ExternalAuthorityTokenProvider.when_getting_access_token;

public class and_discovery_times_out : given.an_external_authority
{
    Exception _result;

    void Establish() => _handler.DiscoveryException = new TaskCanceledException("The request timed out.", new TimeoutException());

    async Task Because() => _result = await Catch.Exception(() => _provider.GetAccessToken());

    [Fact] void should_fail_as_a_discovery_failure() => _result.ShouldBeOfExactType<TokenEndpointDiscoveryFailed>();
    [Fact] void should_name_the_authority() => _result.Message.Contains("https://idp.example.com/realms/chronicle", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_log_a_warning() => _log.Entries.Count(_ => _.Level == LogLevel.Warning).ShouldEqual(1);
}
