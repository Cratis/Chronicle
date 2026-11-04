// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_ExternalAuthorityTokenProvider.when_getting_access_token;

public class and_discovery_keeps_failing : given.an_external_authority
{
    Exception _first;
    Exception _second;
    Exception _third;

    void Establish() => _handler.FailDiscovery = true;

    async Task Because()
    {
        _first = await Catch.Exception(() => _provider.GetAccessToken());
        _time.Advance(TimeSpan.FromSeconds(1));
        _second = await Catch.Exception(() => _provider.GetAccessToken());
        _third = await Catch.Exception(() => _provider.GetAccessToken());
    }

    [Fact] void should_fail_with_a_discovery_failure() => _second.ShouldBeOfExactType<TokenEndpointDiscoveryFailed>();
    [Fact] void should_fail_with_a_discovery_failure_every_time() => _third.ShouldBeOfExactType<TokenEndpointDiscoveryFailed>();
    [Fact] void should_not_keep_a_cause_on_the_first_failure() => _first.InnerException.ShouldBeNull();
    [Fact] void should_not_cache_a_cause_for_the_throttled_failure() => _second.InnerException.ShouldBeNull();
    [Fact] void should_not_cache_a_cause_every_time() => _third.InnerException.ShouldBeNull();
    [Fact] void should_repeat_the_failure_category() => _second.Message.ShouldEqual(_first.Message);
    [Fact] void should_throw_a_new_failure() => ReferenceEquals(_second, _first).ShouldBeFalse();
    [Fact] void should_throw_a_new_failure_every_time() => ReferenceEquals(_third, _second).ShouldBeFalse();
    [Fact] void should_only_discover_once() => _handler.DiscoveryRequests.Count().ShouldEqual(1);
    [Fact] void should_only_log_one_warning() => _log.Entries.Count(_ => _.Level == LogLevel.Warning).ShouldEqual(1);
}
