// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Federation.for_FederationPortPolicy;

public class when_deciding_about_the_serve_service : Specification
{
    const string Service = FederationPortPolicy.ServeServicePathPrefix + "Serve";

    ChronicleOptions _enabled;
    ChronicleOptions _dialOnly;
    bool _onFederationPort;
    bool _withOtherCasingOnFederationPort;
    bool _onMainPort;
    bool _withOtherCasingOnMainPort;
    bool _onHealthPort;
    bool _whenOnlyDialing;
    bool _otherServiceOnFederationPort;

    void Establish()
    {
        _enabled = new() { Federation = new() { Enabled = true, Port = 35100 } };
        _dialOnly = new() { Federation = new() { Enabled = false, Dial = new FederationDialOptions { Enabled = true } } };
    }

    void Because()
    {
        _onFederationPort = FederationPortPolicy.ShouldReject(_enabled, 35100, Service);
        _withOtherCasingOnFederationPort = FederationPortPolicy.ShouldReject(_enabled, 35100, Service.ToLowerInvariant());
        _onMainPort = FederationPortPolicy.ShouldReject(_enabled, 35000, Service);
        _withOtherCasingOnMainPort = FederationPortPolicy.ShouldReject(_enabled, 35000, Service.ToUpperInvariant());
        _onHealthPort = FederationPortPolicy.ShouldReject(_enabled, 35200, Service);
        _whenOnlyDialing = FederationPortPolicy.ShouldReject(_dialOnly, _dialOnly.Port, Service);
        _otherServiceOnFederationPort = FederationPortPolicy.ShouldReject(_enabled, 35100, "/Cratis.Chronicle.Contracts.EventStores.EventStores/GetEventStores");
    }

    [Fact] void should_serve_the_serve_service_on_the_federation_port() => _onFederationPort.ShouldBeFalse();
    [Fact] void should_match_the_route_case_insensitively_on_the_federation_port() => _withOtherCasingOnFederationPort.ShouldBeFalse();
    [Fact] void should_refuse_the_serve_service_on_the_main_port() => _onMainPort.ShouldBeTrue();
    [Fact] void should_refuse_it_with_any_casing_on_the_main_port() => _withOtherCasingOnMainPort.ShouldBeTrue();
    [Fact] void should_refuse_it_on_the_health_port() => _onHealthPort.ShouldBeTrue();
    [Fact] void should_refuse_it_when_the_kernel_only_dials_because_there_is_no_federation_port() => _whenOnlyDialing.ShouldBeTrue();
    [Fact] void should_still_refuse_every_other_service_on_the_federation_port() => _otherServiceOnFederationPort.ShouldBeTrue();
}
