// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.Server.for_ForwardedHeadersTrust;

public class when_binding_proxy_network_configuration : given.a_forwarded_request
{
    Task Because()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{Configuration.ChronicleOptions.SectionPath}:ForwardedHeaders:KnownNetworks:0"] = "192.0.2.0/24"
            })
            .Build();
        return Process(configuration.GetSection(Configuration.ChronicleOptions.SectionPath).Get<Configuration.ChronicleOptions>()!);
    }

    [Fact] void should_trust_the_bound_network() => _context.Request.Scheme.ShouldEqual("https");
}
