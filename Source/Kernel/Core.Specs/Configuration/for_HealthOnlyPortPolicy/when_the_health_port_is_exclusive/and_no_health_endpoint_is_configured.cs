// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_HealthOnlyPortPolicy.when_the_health_port_is_exclusive;

/// <summary>
/// An endpoint that is empty or only whitespace configures no reachable path, so nothing addresses it and the
/// port rejects everything. Resolving it to "/" instead would serve the root - which the fallback answers with
/// the Workbench entry document - and a misconfiguration on a port whose whole purpose is to expose one thing
/// must not open it up.
/// </summary>
public class and_no_health_endpoint_is_configured : Specification
{
    const int HealthPort = 8080;
    ChronicleOptions _empty;
    ChronicleOptions _whitespace;
    bool _rootOnEmpty;
    bool _healthOnEmpty;
    bool _rootOnWhitespace;

    void Establish()
    {
        _empty = new ChronicleOptions
        {
            Port = 35000,
            HealthCheckEndpoint = string.Empty,
            Health = new Health { Port = HealthPort, Exclusive = true }
        };
        _whitespace = new ChronicleOptions
        {
            Port = 35000,
            HealthCheckEndpoint = "   ",
            Health = new Health { Port = HealthPort, Exclusive = true }
        };
    }

    void Because()
    {
        _rootOnEmpty = HealthOnlyPortPolicy.ShouldReject(_empty, HealthPort, "/");
        _healthOnEmpty = HealthOnlyPortPolicy.ShouldReject(_empty, HealthPort, "/health");
        _rootOnWhitespace = HealthOnlyPortPolicy.ShouldReject(_whitespace, HealthPort, "/");
    }

    [Fact] void should_reject_the_root_when_the_endpoint_is_empty() => _rootOnEmpty.ShouldBeTrue();
    [Fact] void should_reject_the_health_path_when_the_endpoint_is_empty() => _healthOnEmpty.ShouldBeTrue();
    [Fact] void should_reject_the_root_when_the_endpoint_is_whitespace() => _rootOnWhitespace.ShouldBeTrue();
}
