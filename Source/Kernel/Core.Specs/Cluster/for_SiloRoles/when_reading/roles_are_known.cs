// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans.Runtime.MembershipService.SiloMetadata;

namespace Cratis.Chronicle.Cluster.for_SiloRoles.when_reading;

public class roles_are_known : Specification
{
    IHost _host;
    SiloRoles _siloRoles;
    SiloAddress _address;
    bool _known;
    ClusterRoles? _roles;

    void Establish()
    {
        _host = new HostBuilder().UseOrleans(silo => silo.UseSiloMetadata(new Dictionary<string, string> { ["cratis.chronicle.roles.v"] = "1", ["cratis.chronicle.role.eventSequences"] = "true", ["cratis.chronicle.role.observers"] = "false", ["cratis.chronicle.role.jobs"] = "false" })).Build();
        _address = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111), 1);
        var cache = Substitute.For<ISiloMetadataCache>();
        cache.GetSiloMetadata(_address).Returns(_host.Services.GetRequiredService<IOptions<SiloMetadata>>().Value);
        _siloRoles = new(cache);
    }

    void Because() => _known = _siloRoles.GetRoles(_address).TryGetValue(out _roles);

    void Destroy() => _host?.Dispose();

    [Fact] void should_return_known_roles() => _known.ShouldBeTrue();
    [Fact] void should_read_the_event_sequences_role() => _roles!.EventSequences.ShouldBeTrue();
    [Fact] void should_read_the_observers_role() => _roles!.Observers.ShouldBeFalse();
    [Fact] void should_read_the_jobs_role() => _roles!.Jobs.ShouldBeFalse();
}
