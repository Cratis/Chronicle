// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Server.for_ForwardedHeadersTrust.when_using_the_framework_forwarding_filter;

[Collection(nameof(given.automatic_forwarding_collection))]
public class and_the_peer_is_a_configured_proxy : given.a_host_with_automatic_forwarding
{
    Task Because() => Send(
        new() { ForwardedHeaders = new() { KnownProxies = ["192.0.2.10", "192.0.2.20"] } },
        "192.0.2.10",
        "198.51.100.42, 192.0.2.20",
        "http, https");

    [Fact] void should_install_the_framework_forwarding_filter() => _automaticFilterRegistered.ShouldBeTrue();
    [Fact] void should_use_the_last_forwarded_scheme_once() => _scheme.ShouldEqual("https");
    [Fact] void should_use_the_last_forwarded_ip_once() => _remoteIp.ShouldEqual(IPAddress.Parse("192.0.2.20"));
}
