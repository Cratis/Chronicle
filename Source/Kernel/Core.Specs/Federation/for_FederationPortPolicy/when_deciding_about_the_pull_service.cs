// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Federation.for_FederationPortPolicy;

public class when_deciding_about_the_pull_service : Specification
{
    const string Service = FederationPortPolicy.ServicePathPrefix + "Pull";

    ChronicleOptions _enabled;
    ChronicleOptions _disabled;
    bool _serviceOnFederationPort;
    bool _serviceWithOtherCasingOnFederationPort;
    bool _serviceOnMainPort;
    bool _serviceWithOtherCasingOnMainPort;
    bool _serviceOnHealthPort;
    bool _serviceWhenDisabled;
    bool _httpRouteOnMainPort;
    bool _httpRouteOnFederationPort;
    bool _httpRouteWithTrailingSlash;

    void Establish()
    {
        _enabled = new() { Federation = new() { Enabled = true, Port = 35100 } };
        _disabled = new();
    }

    void Because()
    {
        _serviceOnFederationPort = FederationPortPolicy.ShouldReject(_enabled, 35100, Service);
        _serviceWithOtherCasingOnFederationPort = FederationPortPolicy.ShouldReject(_enabled, 35100, Service.ToLowerInvariant());
        _serviceOnMainPort = FederationPortPolicy.ShouldReject(_enabled, 35000, Service);
        _serviceWithOtherCasingOnMainPort = FederationPortPolicy.ShouldReject(_enabled, 35000, Service.ToUpperInvariant());
        _serviceOnHealthPort = FederationPortPolicy.ShouldReject(_enabled, 35200, Service);
        _serviceWhenDisabled = FederationPortPolicy.ShouldReject(_disabled, _disabled.Port, Service);
        _httpRouteOnMainPort = FederationPortPolicy.ShouldReject(_enabled, 35000, FederationPortPolicy.HttpRoute);
        _httpRouteOnFederationPort = FederationPortPolicy.ShouldReject(_enabled, 35100, FederationPortPolicy.HttpRoute);
        _httpRouteWithTrailingSlash = FederationPortPolicy.ShouldReject(_enabled, 35000, FederationPortPolicy.HttpRoute + "/");
    }

    [Fact] void should_serve_the_pull_service_on_the_federation_port() => _serviceOnFederationPort.ShouldBeFalse();
    [Fact] void should_match_the_route_case_insensitively_on_the_federation_port() => _serviceWithOtherCasingOnFederationPort.ShouldBeFalse();
    [Fact] void should_refuse_the_pull_service_on_the_main_port() => _serviceOnMainPort.ShouldBeTrue();
    [Fact] void should_refuse_a_differently_cased_pull_service_on_the_main_port() => _serviceWithOtherCasingOnMainPort.ShouldBeTrue();
    [Fact] void should_refuse_the_pull_service_on_the_health_port() => _serviceOnHealthPort.ShouldBeTrue();
    [Fact] void should_refuse_the_pull_service_when_federation_is_disabled() => _serviceWhenDisabled.ShouldBeTrue();
    [Fact] void should_refuse_the_http_route_on_the_main_port() => _httpRouteOnMainPort.ShouldBeTrue();
    [Fact] void should_refuse_the_http_route_on_the_federation_port() => _httpRouteOnFederationPort.ShouldBeTrue();
    [Fact] void should_refuse_the_http_route_with_a_trailing_slash() => _httpRouteWithTrailingSlash.ShouldBeTrue();
}
