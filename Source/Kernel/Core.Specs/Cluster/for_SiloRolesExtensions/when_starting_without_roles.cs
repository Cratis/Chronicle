// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Cluster.for_SiloRolesExtensions;

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

    void Because() => _error = Catch.Exception(() => _host.Services.GetRequiredService<IStartupValidator>().Validate());

    void Destroy() => _host?.Dispose();

    [Fact] void should_refuse_startup_because_of_the_roles() =>
        (_error is AggregateException aggregate ? aggregate.Flatten().InnerExceptions : new[] { _error } as IEnumerable<Exception?>)
            .OfType<OptionsValidationException>()
            .Any(_ => _.OptionsType == typeof(ChronicleOptions))
            .ShouldBeTrue();
}
