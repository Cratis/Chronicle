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

namespace Cratis.Chronicle.Projections.Generations.for_GenerationBuildRecovery.given;

/// <summary>
/// Provides real in-memory binding storage and observer claims for catalog-only recovery.
/// </summary>
public class a_generation_build_recovery : Specification
{
    /// <summary>
    /// The event store owning the builds.
    /// </summary>
    protected const string Store = "store";

    /// <summary>
    /// The operational generation flag.
    /// </summary>
    protected IGenerationEnablement _enablement;

    /// <summary>
    /// The current materialization key resolver.
    /// </summary>
    protected IMaterializationKeys _keys;

    /// <summary>
    /// The real catalog and reservation authority.
    /// </summary>
    protected InMemoryMaterializationBindingStorage _catalog;

    /// <summary>
    /// The intent reader, replaceable to model corrupt or missing storage.
    /// </summary>
    protected IGenerationBuildStorage _builds;

    /// <summary>
    /// The current materialization identity.
    /// </summary>
    protected MaterializationKey _key;

    /// <summary>
    /// The original build request.
    /// </summary>
    protected GenerationBuildRequest _request;

    /// <summary>
    /// The reserved Building record.
    /// </summary>
    protected MaterializationGenerationRecord _reserved;

    /// <summary>
    /// The recovery outcomes.
    /// </summary>
    protected IReadOnlyCollection<RecoverableGenerationBuild> _result;

    /// <summary>
    /// Establishes a bound first generation and an original request without reserving a candidate.
    /// </summary>
    async Task Establish()
    {
        _enablement = Substitute.For<IGenerationEnablement>();
        _enablement.Enabled.Returns(true);
        _key = GenerationExecutions.Key;
        _keys = Substitute.For<IMaterializationKeys>();
        _keys.For(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ProjectionId>())
            .Returns(call => Task.FromResult(_key with { Namespace = call.ArgAt<EventStoreNamespaceName>(1), Projection = call.ArgAt<ProjectionId>(2) }));
        var storage = Substitute.For<IStorage>();
        _catalog = new(claims: new InMemoryObserverIdentityClaims(storage));
        _builds = _catalog;
        var specifications = new GenerationExecutionSpecifications(Globals.JsonSerializerOptions);
        var execution = specifications.Capture(GenerationExecutions.Definition(), GenerationExecutions.ReadModel(), GenerationExecutions.EventSchemas(), new("sink-v1"));
        _request = new(_key, execution, new(Guid.NewGuid()), DateTimeOffset.UnixEpoch.AddHours(7));
        await Adopt(_key);
    }

    /// <summary>
    /// Adopts the serving generation and its execution using the public catalog APIs.
    /// </summary>
    /// <param name="key">The materialization to adopt.</param>
    /// <returns>The adoption task.</returns>
    protected async Task Adopt(MaterializationKey key)
    {
        var first = new MaterializationGenerationRecord(MaterializationGenerationIdentity.For(key.Projection, MaterializationGeneration.First, "models"), MaterializationGenerationState.Active, _request.Execution.Revision, DateTimeOffset.UnixEpoch);
        await _catalog.TryAdoptFirst(Store, key, first);
        await _catalog.TrySetBindingEpoch(Store, key, ServingEpoch.Unset, ServingEpoch.Initial);
        await _catalog.TryAdoptExecution(Store, key, _request.Execution);
    }

    /// <summary>
    /// Reserves an original request with real storage and observer claims.
    /// </summary>
    /// <param name="request">The request to persist.</param>
    /// <returns>The reserved catalog record.</returns>
    protected async Task<MaterializationGenerationRecord> Reserve(GenerationBuildRequest request) =>
        (await _catalog.ReserveNext(Store, request.Key, request.Execution, request.Operation, request.At)).Record!;

    /// <summary>
    /// Reads recoverable intents for the current projection without starting an owner.
    /// </summary>
    /// <returns>The recovery outcomes.</returns>
    protected Task<IReadOnlyCollection<RecoverableGenerationBuild>> Resolve() =>
        new GenerationBuildRecovery(_enablement, _keys, _catalog, _builds).Resolve(Store, _key.Namespace, _key.Projection);
}
