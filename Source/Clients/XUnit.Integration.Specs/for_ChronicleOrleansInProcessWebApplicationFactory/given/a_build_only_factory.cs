// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Chronicle.XUnit.Integration.for_ChronicleOrleansInProcessWebApplicationFactory.given;

class a_build_only_factory(
    Action<IServiceCollection>? configureServices = null,
    IReadOnlyDictionary<string, string?>? configuration = null) : ChronicleOrleansInProcessWebApplicationFactory<object>(
    Substitute.For<IChronicleSetupFixture>(),
    configureServices ?? (_ => { }),
    _ => { },
    _ => { },
    null,
    null,
    configuration ?? new Dictionary<string, string?>(),
    new ContentRoot(Directory.GetCurrentDirectory()))
{
    internal int PipelineRegistrations { get; private set; }

    internal IHost BuildHost(Action<IServiceCollection>? configureApplication = null)
    {
        var builder = CreateHostBuilder();

        // WebApplicationFactory applies its web-host hooks after CreateHostBuilder, before Build.
        // Do not use factory.Services or CreateClient: either would start the silo.
        builder.ConfigureWebHost(webHost =>
        {
            ConfigureWebHost(webHost);
            webHost
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    configureApplication?.Invoke(services);
                    PipelineRegistrations = services.Count(_ => _.ServiceType == typeof(ICommandPipeline));
                });
        });

        return builder.Build();
    }
}
