// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_HealthOnlyPortPolicy.when_the_health_port_is_exclusive;

public class and_the_health_path_varies_in_shape : given.an_exclusive_dedicated_health_port
{
    bool _trailingSlash;
    bool _differentCase;

    void Because()
    {
        _trailingSlash = HealthOnlyPortPolicy.ShouldReject(_options, HealthPort, "/health/");
        _differentCase = HealthOnlyPortPolicy.ShouldReject(_options, HealthPort, "/Health");
    }

    [Fact] void should_serve_a_trailing_slash() => _trailingSlash.ShouldBeFalse();
    [Fact] void should_serve_a_differently_cased_path() => _differentCase.ShouldBeFalse();
}
