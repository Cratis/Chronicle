// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Server.for_ForwardedHeadersTrust.when_using_the_framework_forwarding_filter;

[Collection(nameof(given.automatic_forwarding_collection))]
public class and_the_peer_is_untrusted : given.a_host_with_automatic_forwarding
{
    Task Because() => Send(new(), "192.0.2.10", "198.51.100.42", "https");

    [Fact] void should_install_the_framework_forwarding_filter() => _automaticFilterRegistered.ShouldBeTrue();
    [Fact] void should_not_trust_a_spoofed_https_scheme() => _scheme.ShouldEqual("http");
    [Fact] void should_not_trust_a_spoofed_client_ip() => _remoteIp.ShouldEqual(IPAddress.Parse("192.0.2.10"));
}
