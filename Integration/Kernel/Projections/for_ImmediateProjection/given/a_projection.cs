// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Setup;
using KernelProjection = KernelCore::Cratis.Chronicle.Projections.IProjection;

namespace Cratis.Chronicle.Kernel.Integration.Projections.for_ImmediateProjection.given;

/// <summary>
/// Registers a passive projection and resolves its real kernel grains.
/// </summary>
/// <param name="fixture">The isolated integration fixture.</param>
public class a_projection(DefinitionChangeInterleaveFixture fixture) : Specification<DefinitionChangeInterleaveFixture>(fixture)
{
    protected DefinitionChangeInterleaveGate _gate;
    protected KernelProjection _projection;
    protected KernelCore::Cratis.Chronicle.Projections.IImmediateProjection _immediateProjection;
    protected ProjectionDefinition _changed;

    /// <inheritdoc/>
    public override bool AutoDiscoverArtifacts => false;

    /// <inheritdoc/>
    public override IEnumerable<Type> EventTypes => [typeof(ProjectionReadStarted)];

    /// <inheritdoc/>
    public override IEnumerable<Type> Projections => [typeof(InterleavedProjection)];

    /// <inheritdoc/>
    protected override Action<Configuration.IChronicleBuilder> GetStorageConfigurator(string mongoServer) => builder =>
    {
        builder.WithMongoDB($"mongodb://localhost:{MongoDBContainer.GetMappedPublicPort(27017)}/?directConnection=true", Constants.EventStore);
        builder.SiloBuilder.ConfigureServices(services =>
        {
            services.AddSingleton<DefinitionChangeInterleaveGate>();
            services.AddSingleton<IOutgoingGrainCallFilter, DefinitionChangeInterleaveOutgoingCallFilter>();
        });
    };

    async Task Establish()
    {
        var store = await ChronicleClient.GetEventStore($"interleave-{Guid.NewGuid():N}", "definition-change");
        await store.DiscoverAll();
        await store.RegisterAll();
        (await store.WaitForRegistration(TimeSpan.FromSeconds(30))).IsSuccess.ShouldBeTrue();
        (await store.EventLog.Append("source", new ProjectionReadStarted("projected"))).IsSuccess.ShouldBeTrue();

        var grains = Services.GetRequiredService<IGrainFactory>();
        _gate = Services.GetRequiredService<DefinitionChangeInterleaveGate>();
        _projection = grains.GetGrain<KernelProjection>(new ProjectionKey(InterleavedProjection.ProjectionIdentifier, store.Name.Value));
        var definition = await _projection.GetDefinition();
        _changed = definition with
        {
            InitialModelState = new System.Text.Json.Nodes.JsonObject { ["name"] = string.Empty, ["seed"] = "changed" },
            // InitialModelState alone is intentionally ignored by the definition comparer.
            IsRewindable = !definition.IsRewindable
        };
        _immediateProjection = grains.GetGrain<KernelCore::Cratis.Chronicle.Projections.IImmediateProjection>(
            new ImmediateProjectionKey(InterleavedProjection.ProjectionIdentifier, store.Name.Value, store.Namespace.Value, EventSequenceId.Log, "source"));
        await _immediateProjection.GetModelInstance();
        _gate.Arm();
    }
}
