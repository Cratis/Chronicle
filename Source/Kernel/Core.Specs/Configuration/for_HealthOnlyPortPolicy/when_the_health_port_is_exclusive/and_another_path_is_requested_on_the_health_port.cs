// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_HealthOnlyPortPolicy.when_the_health_port_is_exclusive;

public class and_another_path_is_requested_on_the_health_port : given.an_exclusive_dedicated_health_port
{
    bool _workbenchRoot;
    bool _api;
    bool _identity;
    bool _healthPrefixedPath;

    void Because()
    {
        _workbenchRoot = HealthOnlyPortPolicy.ShouldReject(_options, HealthPort, "/");
        _api = HealthOnlyPortPolicy.ShouldReject(_options, HealthPort, "/api/event-store");
        _identity = HealthOnlyPortPolicy.ShouldReject(_options, HealthPort, "/identity/login");
        _healthPrefixedPath = HealthOnlyPortPolicy.ShouldReject(_options, HealthPort, "/healthy-secrets");
    }

    [Fact] void should_reject_the_workbench_root() => _workbenchRoot.ShouldBeTrue();
    [Fact] void should_reject_the_rest_api() => _api.ShouldBeTrue();
    [Fact] void should_reject_the_identity_endpoints() => _identity.ShouldBeTrue();
    [Fact] void should_reject_a_path_merely_prefixed_by_the_health_path() => _healthPrefixedPath.ShouldBeTrue();
}
