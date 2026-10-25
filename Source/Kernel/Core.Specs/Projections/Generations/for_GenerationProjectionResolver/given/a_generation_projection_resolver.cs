// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Generations.Bindings;
using Cratis.Chronicle.Generations.Enablement;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Storage.Generations.Bindings;
using Cratis.Json;
using EngineProjection = Cratis.Chronicle.Projections.Engine.IProjection;

namespace Cratis.Chronicle.Projections.Generations.for_GenerationProjectionResolver.given;

public class a_generation_projection_resolver : Specification
{
    protected const string Store = "store";
    protected IGenerationEnablement _enablement;
    protected IGenerationBuildStorage _builds;
    protected IWriterGrants _grants;
    protected IMaterializationKeys _keys;
    protected IMaterializationBindingStorage _bindings;
    protected IProjectionFactory _projectionFactory;
    protected GenerationExecutionSpecifications _specifications;
    protected EngineProjection _projection;
    protected MaterializationExecutionSpecification _execution;
    protected GenerationObserverBinding _binding;
    protected GenerationProjectionResolver _resolver;

    void Establish()
    {
        _enablement = Substitute.For<IGenerationEnablement>();
        _enablement.Enabled.Returns(true);
        _builds = Substitute.For<IGenerationBuildStorage>();
        _grants = Substitute.For<IWriterGrants>();
        _keys = Substitute.For<IMaterializationKeys>();
        _bindings = Substitute.For<IMaterializationBindingStorage>();
        _projectionFactory = Substitute.For<IProjectionFactory>();
        _projection = Substitute.For<EngineProjection>();
        _projectionFactory
            .Create(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ReadModelDefinition>(), Arg.Any<IEnumerable<EventTypeSchema>>())
            .Returns(_projection);
        _specifications = new(Globals.JsonSerializerOptions);
        _execution = _specifications.Capture(GenerationExecutions.Definition(), GenerationExecutions.ReadModel(), GenerationExecutions.EventSchemas(), new("sink-v1"));
        _binding = GenerationExecutions.Binding(_execution);
        _keys.For(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ProjectionId>()).Returns(GenerationExecutions.Key);
        _resolver = new(_enablement, _builds, new ForwardingWriterGrants(_grants), _keys, _bindings, _projectionFactory, _specifications);
    }

    protected void Bind(GenerationObserverBinding binding, MaterializationExecutionSpecification? execution = null)
    {
        _builds.ResolveObserver(Store, GenerationExecutions.Namespace, EventSequenceId.Log, binding.Identity.ObserverId).Returns(binding);
        _builds.GetExecution(Store, binding.Key, binding.Identity.Generation).Returns(execution ?? _execution);
    }

    protected Task<GenerationExecution?> Resolve(ObserverId observerId) =>
        _resolver.Resolve(Store, GenerationExecutions.Namespace, EventSequenceId.Log, observerId);
}
