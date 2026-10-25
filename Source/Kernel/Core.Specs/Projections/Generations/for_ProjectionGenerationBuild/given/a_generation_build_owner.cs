// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;
using Cratis.Chronicle.Generations.Bindings;
using Cratis.Chronicle.Generations.Enablement;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Generations.Bindings;
using Cratis.Chronicle.Storage.InMemory.Generations.Bindings;
using Cratis.Json;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.Generations.for_ProjectionGenerationBuild.given;

/// <summary>
/// Provides a real binding catalog and identity claims for owner admission.
/// </summary>
public class a_generation_build_owner : Specification
{
    /// <summary>The event store owning the build.</summary>
    protected const string Store = "store";

    /// <summary>The operational feature flag.</summary>
    protected IGenerationEnablement _enablement;

    /// <summary>The current key resolver.</summary>
    protected IMaterializationKeys _keys;

    /// <summary>The storage graph used to check existing observer definitions.</summary>
    protected IStorage _storage;

    /// <summary>The real generation catalog and writer authority.</summary>
    protected InMemoryMaterializationBindingStorage _catalog;

    /// <summary>The reservation collaborator, replaceable for provider refusal.</summary>
    protected IGenerationBuildStorage _builds;

    /// <summary>The immutable execution restorer.</summary>
    protected GenerationExecutionSpecifications _specifications;

    /// <summary>The request's materialization identity.</summary>
    protected MaterializationKey _key;

    /// <summary>The adopted serving generation.</summary>
    protected MaterializationGenerationRecord _first;

    /// <summary>The persisted operation request.</summary>
    protected GenerationBuildRequest _request;

    /// <summary>The resulting owner admission.</summary>
    protected GenerationBuildAdmission _result;

    /// <summary>
    /// Establishes an epoch-bound first generation with immutable execution inputs.
    /// </summary>
    async Task Establish()
    {
        _enablement = Substitute.For<IGenerationEnablement>();
        _enablement.Enabled.Returns(true);
        _key = GenerationExecutions.Key;
        _keys = Substitute.For<IMaterializationKeys>();
        _keys.For(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ProjectionId>()).Returns(_ => Task.FromResult(_key));
        _storage = Substitute.For<IStorage>();
        var claims = new InMemoryObserverIdentityClaims(_storage);
        _catalog = new(claims: claims);
        _builds = _catalog;
        _specifications = new(Globals.JsonSerializerOptions);
        var execution = _specifications.Capture(GenerationExecutions.Definition(), GenerationExecutions.ReadModel(), GenerationExecutions.EventSchemas(), new("sink-v1"));
        _request = new(_key, execution, new(Guid.NewGuid()), DateTimeOffset.UnixEpoch);
        _first = new(MaterializationGenerationIdentity.For(_key.Projection, MaterializationGeneration.First, "models"), MaterializationGenerationState.Active, execution.Revision, DateTimeOffset.UnixEpoch);
        await _catalog.TryAdoptFirst(Store, _key, _first);
        await _catalog.TrySetBindingEpoch(Store, _key, ServingEpoch.Unset, ServingEpoch.Initial);
        await _builds.TryAdoptExecution(Store, _key, execution);
        _keys.ClearReceivedCalls();
        _storage.ClearReceivedCalls();
    }

    /// <summary>
    /// Starts the request on a fresh activation, sharing only durable collaborators with earlier starts.
    /// </summary>
    /// <returns>The owner admission.</returns>
    protected async Task<GenerationBuildAdmission> Start()
    {
        var silo = new TestKitSilo();
        silo.AddService(_enablement);
        silo.AddService(_keys);
        silo.AddService(_specifications);
        silo.AddService<IMaterializationBindingStorage>(_catalog);
        silo.AddService(_builds);
        silo.AddService<IGenerationWriterGrants>(_catalog);
        var owner = await silo.CreateGrainAsync<ProjectionGenerationBuild>($"{Store}/{_request.Key.Value}");
        return await owner.Start(_request);
    }

    /// <summary>
    /// Reserves the operation directly to model a crash before owner admission.
    /// </summary>
    /// <returns>The durable reservation.</returns>
    protected Task<GenerationReservation> Reserve() => _builds.ReserveNext(Store, _request.Key, _request.Execution, _request.Operation, _request.At);

    /// <summary>
    /// Reads the authoritative materialization after the action.
    /// </summary>
    /// <returns>The binding catalog.</returns>
    protected async Task<MaterializationBinding> Binding() => (await _catalog.Get(Store, _key))!;
}
