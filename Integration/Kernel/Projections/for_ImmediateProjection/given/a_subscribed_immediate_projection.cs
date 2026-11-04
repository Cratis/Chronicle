// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Setup;

namespace Cratis.Chronicle.Kernel.Integration.Projections.for_ImmediateProjection.given;

/// <summary>
/// Registers a passive projection, activates its immediate projection subscriber and resolves the real kernel grains.
/// </summary>
/// <param name="fixture">The isolated integration fixture.</param>
public class a_subscribed_immediate_projection(SubscriberLifecycleFixture fixture) : Specification<SubscriberLifecycleFixture>(fixture)
{
    protected SubscriberLifecycleGate _gate;
    protected IImmediateProjection _immediateProjection;
    protected Func<Task> _changeDefinition;

    /// <inheritdoc/>
    public override bool AutoDiscoverArtifacts => false;

    /// <inheritdoc/>
    public override IEnumerable<Type> EventTypes => [typeof(ProjectionReadStarted)];

    /// <inheritdoc/>
    public override IEnumerable<Type> Projections => [];

    /// <inheritdoc/>
    protected override Action<Configuration.IChronicleBuilder> GetStorageConfigurator(string mongoServer) => builder =>
    {
        builder.WithMongoDB($"mongodb://localhost:{MongoDBContainer.GetMappedPublicPort(27017)}/?directConnection=true", Constants.EventStore);
        builder.SiloBuilder.ConfigureServices(services =>
        {
            services.AddSingleton<SubscriberLifecycleGate>();
            services.AddSingleton<IOutgoingGrainCallFilter, SubscriberLifecycleOutgoingCallFilter>();
        });
    };

    async Task Establish()
    {
        var store = await ChronicleClient.GetEventStore($"lifecycle-{Guid.NewGuid():N}", "definition-change");
        await store.DiscoverAll();
        await store.RegisterAll();
        (await store.WaitForRegistration(TimeSpan.FromSeconds(30))).IsSuccess.ShouldBeTrue();
        await store.Projections.Register<InterleavedReadModel>(new InterleavedProjection().Define, InterleavedProjection.ProjectionIdentifier);
        (await store.EventLog.Append("source", new ProjectionReadStarted("projected"))).IsSuccess.ShouldBeTrue();

        var grains = Services.GetRequiredService<IGrainFactory>();
        _gate = Services.GetRequiredService<SubscriberLifecycleGate>();
        _changeDefinition = () => store.Projections.Register<InterleavedReadModel>(
            builder =>
            {
                new InterleavedProjection().Define(builder);
                var initial = new InterleavedReadModel(string.Empty, "changed");
                builder.WithInitialValues(() => initial);

                // InitialModelState alone is intentionally ignored by the definition comparer.
                builder.NotRewindable();
            },
            InterleavedProjection.ProjectionIdentifier);
        _immediateProjection = grains.GetGrain<IImmediateProjection>(
            new ImmediateProjectionKey(InterleavedProjection.ProjectionIdentifier, store.Name.Value, store.Namespace.Value, EventSequenceId.Log, "source"));
        var warm = await _immediateProjection.GetModelInstance();
        ValidateReadModel(warm.HasReadModel, warm.ReadModel, "warm-up", "original");
    }

    protected InterleavedReadModel ValidateReadModel(bool hasReadModel, JsonObject? model, string phase, string expectedSeed)
    {
        var keys = model is null ? "" : string.Join(", ", model.Take(8).Select(entry => entry.Key.Length > 64 ? entry.Key[..64] : entry.Key));
        var diagnostic = $"{phase}: HasReadModel={hasReadModel}; model null: {model is null}; property count: {model?.Count ?? 0}; first 8 keys (up to 64 characters each): [{keys}]";
        Assert.True(hasReadModel, diagnostic);
        Assert.True(model is not null, diagnostic);
        var readModel = model;
        foreach (var property in new[] { nameof(InterleavedReadModel.Name), nameof(InterleavedReadModel.Seed) })
        {
            Assert.True(readModel.TryGetPropertyValue(property, out var value), $"Expected property '{property}'. {diagnostic}");
            Assert.True(value is not null, $"Expected non-null property '{property}'. {diagnostic}");
        }

        var typed = readModel.Deserialize<InterleavedReadModel>(ChronicleClient.Options.JsonSerializerOptions);
        Assert.True(typed is not null, diagnostic);
        Assert.True(typed.Name == "projected", diagnostic);
        Assert.True(typed.Seed == expectedSeed, diagnostic);
        return typed;
    }
}
