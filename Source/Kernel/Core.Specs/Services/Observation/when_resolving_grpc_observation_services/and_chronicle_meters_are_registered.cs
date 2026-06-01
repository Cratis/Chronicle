// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Observation.Reactors.Clients;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;
using ReactorsService = Cratis.Chronicle.Services.Observation.Reactors.Reactors;
using ReducersService = Cratis.Chronicle.Services.Observation.Reducers.Reducers;

namespace Cratis.Chronicle.Services.Observation.when_resolving_grpc_observation_services;

public class and_chronicle_meters_are_registered : Specification
{
    ServiceProvider _serviceProvider;
    Exception? _reactorsException;
    Exception? _reducersException;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddChronicleMeters();
        services.AddSingleton(Substitute.For<IGrainFactory>());
        services.AddSingleton(Substitute.For<IReactorMediator>());
        services.AddSingleton(Substitute.For<IReducerMediator>());
        services.AddSingleton(Substitute.For<IStorage>());
        services.AddSingleton(Substitute.For<IExpandoObjectConverter>());
        services.AddSingleton(new JsonSerializerOptions());
        services.AddSingleton<ReactorsService>();
        services.AddSingleton<ReducersService>();

        _serviceProvider = services.BuildServiceProvider();
    }

    void Because()
    {
        try
        {
            _ = _serviceProvider.GetRequiredService<ReactorsService>();
        }
        catch (Exception ex)
        {
            _reactorsException = ex;
        }

        try
        {
            _ = _serviceProvider.GetRequiredService<ReducersService>();
        }
        catch (Exception ex)
        {
            _reducersException = ex;
        }
    }

    void Destroy() => _serviceProvider.Dispose();

    [Fact] void should_resolve_reactors_service() => _reactorsException.ShouldBeNull();
    [Fact] void should_resolve_reducers_service() => _reducersException.ShouldBeNull();
}
