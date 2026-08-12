// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_HealthOnlyPortPolicy.when_the_health_port_is_exclusive;

public class and_the_request_is_on_the_main_port : given.an_exclusive_dedicated_health_port
{
    bool _workbenchRoot;
    bool _api;

    void Because()
    {
        _workbenchRoot = HealthOnlyPortPolicy.ShouldReject(_options, MainPort, "/");
        _api = HealthOnlyPortPolicy.ShouldReject(_options, MainPort, "/api/event-store");
    }

    [Fact] void should_serve_the_workbench_root() => _workbenchRoot.ShouldBeFalse();
    [Fact] void should_serve_the_rest_api() => _api.ShouldBeFalse();
}
