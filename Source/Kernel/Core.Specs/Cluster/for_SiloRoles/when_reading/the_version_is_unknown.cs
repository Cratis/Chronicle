// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans.Runtime.MembershipService.SiloMetadata;

namespace Cratis.Chronicle.Cluster.for_SiloRoles.when_reading;

public class the_version_is_unknown : Specification
{
    IHost _host;
    SiloRoles _siloRoles;
    SiloAddress _address;
    bool _known;

    void Establish()
    {
        _host = new HostBuilder().UseOrleans(silo => silo.UseSiloMetadata(new Dictionary<string, string> { ["cratis.chronicle.roles.v"] = "2" })).Build();
        _address = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111), 1);
        var cache = Substitute.For<ISiloMetadataCache>();
        cache.GetSiloMetadata(_address).Returns(_host.Services.GetRequiredService<IOptions<SiloMetadata>>().Value);
        _siloRoles = new(cache);
    }

    void Because() => _known = _siloRoles.GetRoles(_address).TryGetValue(out _);

    void Destroy() => _host?.Dispose();

    [Fact] void should_return_unknown_roles() => _known.ShouldBeFalse();
}
