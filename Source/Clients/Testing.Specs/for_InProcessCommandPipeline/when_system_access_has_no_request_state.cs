// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelStorage;

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using KernelSecurity = KernelCore::Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Testing.for_InProcessCommandPipeline;

public class when_system_access_has_no_request_state : Specification
{
    [Fact]
    public async Task should_refuse_the_operation_without_using_root_request_state()
    {
        var pipeline = InProcessCommandPipeline.Create(
            Substitute.For<IGrainFactory>(),
            Substitute.For<KernelStorage::Cratis.Chronicle.Storage.IStorage>(),
            new JsonSerializerOptions(),
            services =>
            {
                services.AddSingleton(Options.Create(new KernelCore::Cratis.Chronicle.Configuration.ChronicleOptions()));
                KernelSecurity.AdministratorAuthorizationServiceCollectionExtensions.AddChronicleAdministratorAuthorization(services);
                services.AddScoped(_ => Substitute.For<KernelSecurity.IAdministratorAuthorization>());
            });
        var result = await pipeline.Execute(new KernelCore::Cratis.Chronicle.Observation.RetryPartition("System", "Default", "observer", "event-log", "partition"));
        result.IsAuthorized.ShouldBeFalse();
        result.ExceptionMessages.ShouldBeEmpty();
    }
}
