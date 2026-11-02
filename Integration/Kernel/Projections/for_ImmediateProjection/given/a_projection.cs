// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Registrations;
using Cratis.Chronicle.Setup;

namespace Cratis.Chronicle.Kernel.Integration.Projections.for_ImmediateProjection.given;

/// <summary>
/// Registers a passive projection and resolves its real kernel grains.
/// </summary>
/// <param name="fixture">The isolated integration fixture.</param>
public class a_projection(DefinitionChangeInterleaveFixture fixture) : Specification<DefinitionChangeInterleaveFixture>(fixture)
{
    protected DefinitionChangeInterleaveGate _gate;
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
        await store.Projections.Register<InterleavedReadModel>(new InterleavedProjection().Define, InterleavedProjection.ProjectionIdentifier);
        (await store.EventLog.Append("source", new ProjectionReadStarted("projected"))).IsSuccess.ShouldBeTrue();

        _changeDefinition = () => store.Projections.Register<InterleavedReadModel>(
            builder =>
            {
                new InterleavedProjection().Define(builder);
                builder.WithInitialValues(() => new InterleavedReadModel(string.Empty, "changed"));

                // InitialModelState alone is intentionally ignored by the definition comparer.
                builder.NotRewindable();
            },
            InterleavedProjection.ProjectionIdentifier);

        var grains = Services.GetRequiredService<IGrainFactory>();
        _gate = Services.GetRequiredService<DefinitionChangeInterleaveGate>();
        _immediateProjection = grains.GetGrain<IImmediateProjection>(
            new ImmediateProjectionKey(InterleavedProjection.ProjectionIdentifier, store.Name.Value, store.Namespace.Value, EventSequenceId.Log, "source"));
        await _immediateProjection.GetModelInstance();
        _gate.Arm();
    }
}
