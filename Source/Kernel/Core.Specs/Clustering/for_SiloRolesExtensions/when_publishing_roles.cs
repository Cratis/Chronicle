// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans.Runtime.MembershipService.SiloMetadata;

namespace Cratis.Chronicle.Clustering.for_SiloRolesExtensions;

public class when_publishing_roles : Specification
{
    IHost _host;
    SiloMetadata _metadata;

    void Establish() => _host = new HostBuilder().UseOrleans(silo =>
    {
        silo.UseSiloMetadata(new Dictionary<string, string> { ["host.before"] = "kept" });
        silo.AddChronicleSiloRoles();
        silo.UseSiloMetadata(new Dictionary<string, string> { ["host.after"] = "also kept" });
        silo.Services.Configure<ChronicleOptions>(options =>
        {
            options.Clustering.Roles.EventSequences = false;
            options.Clustering.Roles.Observers = false;
        });
    }).Build();

    void Because() => _metadata = _host.Services.GetRequiredService<IOptions<SiloMetadata>>().Value;

    void Destroy() => _host?.Dispose();

    [Fact] void should_preserve_existing_host_metadata() => _metadata.Metadata["host.before"].ShouldEqual("kept");
    [Fact] void should_preserve_later_host_metadata() => _metadata.Metadata["host.after"].ShouldEqual("also kept");
    [Fact] void should_publish_the_version() => _metadata.Metadata["cratis.chronicle.roles.v"].ShouldEqual("1");
    [Fact] void should_publish_the_final_event_sequences_role() => _metadata.Metadata["cratis.chronicle.role.eventSequences"].ShouldEqual("false");
    [Fact] void should_publish_the_final_observers_role() => _metadata.Metadata["cratis.chronicle.role.observers"].ShouldEqual("false");
    [Fact] void should_publish_the_jobs_role() => _metadata.Metadata["cratis.chronicle.role.jobs"].ShouldEqual("true");
}
