// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Setup.Serialization;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Storage;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_ensuring;

public class and_registration_is_blocked : Specification
{
    readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    IHost _host;
    IReadModelsManager _manager;
    Task _registration;
    Task[] _queued;

    async Task Establish()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var storage = new blocked_storage(_entered, _release);
        _host = new HostBuilder().UseOrleans(silo =>
        {
            silo.ConfigureSerialization();
            silo.UseLocalhostClustering(port, 0, serviceId: Guid.NewGuid().ToString(), clusterId: Guid.NewGuid().ToString());
            silo.ConfigureServices(services =>
            {
                services.AddKeyedSingleton<IGrainStorage>(WellKnownGrainStorageProviders.ReadModelsManager, storage);
                services.AddKeyedSingleton<IGrainStorage>(WellKnownGrainStorageProviders.ReadModels, Substitute.For<IGrainStorage>());
                services.AddSingleton(Substitute.For<IProjectionPipelineManager>());
                services.AddSingleton(Substitute.For<IExpandoObjectConverter>());
                services.AddSingleton(Substitute.For<IStorage>());
            });
        }).Build();
        await _host.StartAsync();
        _manager = _host.Services.GetRequiredService<IGrainFactory>().GetGrain<IReadModelsManager>("contention");
        _registration = _manager.Register([given.a_read_models_manager.DefinitionFor("blocked", "Blocked")]);
        await Task.WhenAny(_entered.Task, _registration).WaitAsync(TimeSpan.FromSeconds(10));
        if (_registration.IsCompleted) await _registration;
        await _entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        _queued = Enumerable.Range(0, 51).Select(_ => _manager.Register([])).ToArray();
    }

    async Task Because() => await _manager.Ensure().WaitAsync(TimeSpan.FromSeconds(5));

    [Fact] void should_complete_without_releasing_registration() => _registration.IsCompleted.ShouldBeFalse();
    [Fact] void should_keep_mutations_serialized() => _queued.Any(_ => _.IsCompleted).ShouldBeFalse();

    async Task Destroy()
    {
        _release.TrySetResult();
        await _registration.WaitAsync(TimeSpan.FromSeconds(10));
        await Task.WhenAll(_queued).WaitAsync(TimeSpan.FromSeconds(10));
        await _host.StopAsync();
        _host.Dispose();
    }

    class blocked_storage(TaskCompletionSource entered, TaskCompletionSource release) : IGrainStorage
    {
        public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => Task.CompletedTask;
        public Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => Task.CompletedTask;
        public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            entered.TrySetResult();
            await release.Task;
        }
    }
}
