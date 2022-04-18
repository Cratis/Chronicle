// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Orleans;
using Orleans.Core;
using Orleans.Runtime;
using Orleans.Streams;
using Orleans.Timers;

namespace Aksio.Cratis.Common.Grains;

public abstract class GrainSpecification<TState> : Specification
    where TState : new()
{
    protected Mock<IGrainIdentity> grain_identity;
    protected Mock<IGrainRuntime> runtime;
    protected Mock<IStorage<TState>> storage;
    protected Mock<IServiceProvider> service_provider;
    protected Mock<IKeyedServiceCollection<string, IStreamProvider>> stream_provider_collection;
    protected Mock<IReminderRegistry> reminder_registry;
    protected Mock<ITimerRegistry> timer_registry;
    protected TState state;
    protected TState state_on_write;
    protected Mock<IGrainFactory> grain_factory;

    protected abstract Grain GetGrainInstance();

    protected virtual void OnBeforeGrainActivate()
    {
    }

    void Establish()
    {
        var grain = GetGrainInstance();

        var identityField = typeof(Grain).GetField("Identity", BindingFlags.Instance | BindingFlags.NonPublic);
        grain_identity = new Mock<IGrainIdentity>();
        identityField.SetValue(grain, grain_identity.Object);

        var runtimeProperty = typeof(Grain).GetProperty("Runtime", BindingFlags.Instance | BindingFlags.NonPublic);
        runtime = new Mock<IGrainRuntime>();
        runtimeProperty.SetValue(grain, runtime.Object);

        grain_factory = new();
        runtime.SetupGet(_ => _.GrainFactory).Returns(grain_factory.Object);

        reminder_registry = new();
        runtime.SetupGet(_ => _.ReminderRegistry).Returns(reminder_registry.Object);

        timer_registry = new();
        runtime.SetupGet(_ => _.TimerRegistry).Returns(timer_registry.Object);

        service_provider = new Mock<IServiceProvider>();
        runtime.SetupGet(_ => _.ServiceProvider).Returns(service_provider.Object);

        stream_provider_collection = new Mock<IKeyedServiceCollection<string, IStreamProvider>>();
        service_provider.Setup(_ => _.GetService(typeof(IKeyedServiceCollection<string, IStreamProvider>))).Returns(stream_provider_collection.Object);

        var storageProperty = typeof(Grain<TState>).GetField("storage", BindingFlags.Instance | BindingFlags.NonPublic);
        storage = new Mock<IStorage<TState>>();
        storageProperty.SetValue(grain, storage.Object);

        state = new TState();
        storage.SetupGet(_ => _.State).Returns(state);

        OnBeforeGrainActivate();

        storage.Setup(_ => _.WriteStateAsync()).Returns(() =>
        {
            var serialized = JsonSerializer.Serialize(state);
            state_on_write = JsonSerializer.Deserialize<TState>(serialized);
            return Task.CompletedTask;
        });

        grain.OnActivateAsync();
    }
}
