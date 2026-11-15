// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Federation.for_FederationPortPolicy;

public class when_deciding : Specification
{
    ChronicleOptions _enabled;
    ChronicleOptions _disabled;
    bool _adminOnFederationPort;
    bool _workbenchOnFederationPort;
    bool _serviceOnFederationPort;
    bool _adminOnMainPort;
    bool _anythingWhenDisabled;

    void Establish()
    {
        _enabled = new() { Federation = new() { Enabled = true, Port = 35100 } };
        _disabled = new();
    }

    void Because()
    {
        _adminOnFederationPort = FederationPortPolicy.ShouldReject(_enabled, 35100, "/api/users");
        _workbenchOnFederationPort = FederationPortPolicy.ShouldReject(_enabled, 35100, "/");
        _serviceOnFederationPort = FederationPortPolicy.ShouldReject(_enabled, 35100, FederationPortPolicy.ServicePathPrefix + "Link/Pull");
        _adminOnMainPort = FederationPortPolicy.ShouldReject(_enabled, 35000, "/api/users");
        _anythingWhenDisabled = FederationPortPolicy.ShouldReject(_disabled, 35100, "/api/users");
    }

    [Fact] void should_reject_the_api_on_the_federation_port() => _adminOnFederationPort.ShouldBeTrue();
    [Fact] void should_reject_the_workbench_on_the_federation_port() => _workbenchOnFederationPort.ShouldBeTrue();
    [Fact] void should_serve_the_federation_service_on_the_federation_port() => _serviceOnFederationPort.ShouldBeFalse();
    [Fact] void should_not_touch_the_main_port() => _adminOnMainPort.ShouldBeFalse();
    [Fact] void should_not_reject_anything_when_federation_is_disabled() => _anythingWhenDisabled.ShouldBeFalse();
}
