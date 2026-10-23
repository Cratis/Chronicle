// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Clustering.for_SiloRolesExtensions;

public class when_starting_without_roles : Specification
{
    IHost _host;
    Exception? _error;

    void Establish() => _host = new HostBuilder().UseOrleans(silo =>
    {
        silo.AddChronicleSiloRoles();
        silo.Services.Configure<ChronicleOptions>(options =>
        {
            options.Clustering.Roles.EventSequences = false;
            options.Clustering.Roles.Observers = false;
            options.Clustering.Roles.Jobs = false;
        });
    }).Build();

    async Task Because() => _error = await Catch.Exception(() => _host.StartAsync());

    void Destroy() => _host?.Dispose();

    [Fact] void should_refuse_startup() => _error.ShouldBeOfExactType<OptionsValidationException>();
}
