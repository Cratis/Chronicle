// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text;
using System.Text.Json;
using Orleans.Core;
using Orleans.Runtime;
using Orleans.Streams;
using Orleans.Timers;

namespace Aksio.Cratis.Common.Grains;

public abstract class GrainSpecification<TState> : GrainSpecification
    where TState : new()
{
    protected Mock<IStorage<TState>> storage;
    protected TState state;
    protected List<TState> written_states = new();
    protected TState most_recent_written_state;
    protected Type grain_type = typeof(Grain<TState>);


    protected override void OnStateManagement()
    {
        state ??= new TState();

        var storageProperty = grain_type.GetField("storage", BindingFlags.FlattenHierarchy |
                                                             BindingFlags.Instance |
                                                             BindingFlags.NonPublic);

        if (storageProperty is null)
            throw new MissingMemberException(grain.GetType().Name, "storage");

        storage = new Mock<IStorage<TState>>();
        storageProperty.SetValue(grain, storage.Object);

        storage.SetupGet(_ => _.State).Returns(state);
        storage.Setup(_ => _.WriteStateAsync()).Returns(() =>
        {
            var serialized = JsonSerializer.Serialize(state);
            most_recent_written_state = JsonSerializer.Deserialize<TState>(serialized);
            written_states.Add(most_recent_written_state);
            return Task.CompletedTask;
        });
    }
}

public abstract class GrainSpecification : Specification
{
    protected Mock<IGrainRuntime> runtime;
    protected Mock<IGrainContext> grain_context;
    protected Mock<IServiceProvider> service_provider;
    protected Mock<IKeyedServiceCollection<string, IStreamProvider>> stream_provider_collection;
    protected Mock<ITimerRegistry> timer_registry;
    protected Mock<IGrainFactory> grain_factory;
    protected Grain grain;

    protected abstract Guid GrainId { get; }
    protected abstract string GrainKeyExtension { get; }

    protected abstract Grain GetGrainInstance();

    protected virtual void OnBeforeGrainActivate()
    {
    }

    protected virtual void OnStateManagement()
    {
    }

    void Establish()
    {
        grain = GetGrainInstance();
        grain_context = new();
        grain_context.SetupGet(_ => _.GrainId).Returns(() =>
        {
            var grainType = grain.GetType().FullName;
            var grainTypeBytes = Encoding.UTF8.GetBytes(grainType);
            var grainIdentifier = GrainId.ToString().Replace("-", "");
            if (!string.IsNullOrEmpty(GrainKeyExtension))
            {
                grainIdentifier = $"{grainIdentifier}+{GrainKeyExtension}";
            }
            var grainIdentifierBytes = Encoding.UTF8.GetBytes(grainIdentifier);
            return new GrainId(new GrainType(grainTypeBytes), new IdSpan(grainIdentifierBytes));
        });
        service_provider = new Mock<IServiceProvider>();
        grain_context.SetupGet(_ => _.ActivationServices).Returns(service_provider.Object);

        runtime = new Mock<IGrainRuntime>();
        service_provider.Setup(_ => _.GetService(typeof(IGrainRuntime))).Returns(runtime.Object);

        var runtimeField = typeof(Grain).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Single(_ => _.FieldType == typeof(IGrainRuntime));
        runtimeField.SetValue(grain, runtime.Object);

        var grainContextProperty = typeof(Grain).GetProperty("GrainContext", BindingFlags.Instance | BindingFlags.NonPublic);
        grainContextProperty.SetValue(grain, grain_context.Object);

        grain_factory = new();
        runtime.SetupGet(_ => _.GrainFactory).Returns(grain_factory.Object);

        timer_registry = new();
        runtime.SetupGet(_ => _.TimerRegistry).Returns(timer_registry.Object);

        stream_provider_collection = new Mock<IKeyedServiceCollection<string, IStreamProvider>>();
        service_provider.Setup(_ => _.GetService(typeof(IKeyedServiceCollection<string, IStreamProvider>))).Returns(stream_provider_collection.Object);

        OnStateManagement();
        OnBeforeGrainActivate();

        GrainReferenceExtensions.GetReferenceOverride = (grain) => grain;

        grain.OnActivateAsync(CancellationToken.None);
    }
}
