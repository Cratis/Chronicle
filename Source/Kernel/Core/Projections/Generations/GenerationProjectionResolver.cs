// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;
using Cratis.Chronicle.Generations.Bindings;
using Cratis.Chronicle.Generations.Enablement;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Storage.Generations.Bindings;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Projections.Generations;

/// <summary>
/// Represents an implementation of <see cref="IGenerationProjectionResolver"/>.
/// </summary>
/// <param name="enablement">The <see cref="IGenerationEnablement"/> deciding whether generations are active.</param>
/// <param name="builds">The <see cref="IGenerationBuildStorage"/> resolving observers and execution documents.</param>
/// <param name="grants">The <see cref="IGenerationWriterGrants"/> reading the owner's admitted grant.</param>
/// <param name="keys">The <see cref="IMaterializationKeys"/> resolving materialization keys.</param>
/// <param name="bindings">The <see cref="IMaterializationBindingStorage"/> holding the generation catalog.</param>
/// <param name="projectionFactory">The <see cref="IProjectionFactory"/> building engine projections from snapshots.</param>
/// <param name="specifications">The <see cref="GenerationExecutionSpecifications"/> restoring snapshots.</param>
/// <remarks>
/// Registered explicitly with the generation catalog, never by convention: providers without a catalog must not see it.
/// </remarks>
[IgnoreConvention]
public sealed class GenerationProjectionResolver(
    IGenerationEnablement enablement,
    IGenerationBuildStorage builds,
    IGenerationWriterGrants grants,
    IMaterializationKeys keys,
    IMaterializationBindingStorage bindings,
    IProjectionFactory projectionFactory,
    GenerationExecutionSpecifications specifications) : IGenerationProjectionResolver
{
    /// <inheritdoc/>
    public async Task<GenerationExecution?> Resolve(EventStoreName store, EventStoreNamespaceName ns, EventSequenceId eventSequence, ObserverId observerId)
    {
        if (!enablement.Enabled) return null;
        var binding = await builds.ResolveObserver(store, ns, eventSequence, observerId);
        if (binding?.Identity.Generation.IsFirst != false) return null;
        var snapshot = await builds.GetExecution(store, binding.Key, binding.Identity.Generation) ?? throw new InvalidMaterializationExecution();
        GenerationObserverBindings.Verify(binding, snapshot, ns, eventSequence, observerId);
        var inputs = specifications.Restore(snapshot);
        if (inputs.Projection.Identifier != binding.Key.Projection || inputs.Projection.EventSequenceId != binding.EventSequence) throw new InvalidMaterializationExecution();
        var projection = await projectionFactory.Create(store, ns, inputs.Projection, inputs.ReadModel, inputs.EventSchemas);
        return new GenerationExecution(binding, snapshot, inputs, projection);
    }

    /// <inheritdoc/>
    public async Task<GenerationSinkContext> Admit(EventStoreName store, GenerationExecution execution)
    {
        var grant = await grants.GetAdmitted(store, execution.Binding) ?? throw new GenerationWriterGrantNotAdmitted();
        return new GenerationSinkContext(execution.Binding, execution.Execution, grant);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<GenerationExecution>> ResolveCataloged(EventStoreName store, EventStoreNamespaceName ns, ProjectionId projection)
    {
        if (!enablement.Enabled) return [];
        MaterializationKey key;
        try
        {
            key = await keys.For(store, ns, projection);
        }
        catch (GenerationFloorNotEstablished)
        {
            return [];
        }

        var binding = await bindings.Get(store, key);
        if (binding is null) return [];
        var executions = new List<GenerationExecution>();
        foreach (var record in binding.Generations.Where(IsFollowed))
        {
            var snapshot = await builds.GetExecution(store, key, record.Identity.Generation) ?? throw new InvalidMaterializationExecution();
            var execution = await Resolve(store, ns, snapshot.EventSequence, record.Identity.ObserverId);
            if (execution is not null) executions.Add(execution);
        }

        return executions;
    }

    static bool IsFollowed(MaterializationGenerationRecord record) =>
        !record.Identity.Generation.IsFirst
        && record.State is MaterializationGenerationState.Following or MaterializationGenerationState.Active or MaterializationGenerationState.Retired;
}
