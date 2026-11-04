// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_ExternalAuthorityTokenProvider.when_getting_access_token;

public class and_the_caller_cancels_during_discovery : given.an_external_authority
{
    CancellationTokenSource _cancellation;
    Exception _result;

    void Establish()
    {
        _cancellation = new CancellationTokenSource();
        _handler.OnDiscovery = _cancellation.Cancel;
        _handler.DiscoveryException = new OperationCanceledException(_cancellation.Token);
    }

    async Task Because() => _result = await Catch.Exception(() => _provider.GetAccessToken(_cancellation.Token));

    void Destroy() => _cancellation.Dispose();

    [Fact] void should_propagate_the_cancellation() => (_result is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_not_be_a_discovery_failure() => (_result is TokenEndpointDiscoveryFailed).ShouldBeFalse();
    [Fact] void should_not_log_a_warning() => _log.Entries.Any(_ => _.Level == LogLevel.Warning).ShouldBeFalse();
}
