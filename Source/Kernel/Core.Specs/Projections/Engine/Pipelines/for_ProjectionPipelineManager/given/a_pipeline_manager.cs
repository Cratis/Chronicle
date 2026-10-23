// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager.given;

public class a_pipeline_manager : Specification
{
    protected readonly EventStoreName _store = "store";
    protected readonly EventStoreNamespaceName _namespace = "namespace";
    protected ProjectionPipelineManager _manager;
    protected IProjection _projection;
    protected IStorage _storage;
    protected ISink _sink;
    protected IGrainFactory _grainFactory;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _sink = Substitute.For<ISink>();
        _projection = Substitute.For<IProjection>();
        _projection.Identifier.Returns(new ProjectionId("projection"));
        _projection.EventSequenceId.Returns(EventSequenceId.Log);
        _projection.DefinitionFingerprint.Returns("first");
        _storage.GetEventStore(_store).GetNamespace(_namespace).Sinks.GetFor(_projection.ReadModel).Returns(_sink);
        _grainFactory = Substitute.For<IGrainFactory>();
        _manager = new(
            _storage,
            _grainFactory,
            Substitute.For<IObjectComparer>(),
            Substitute.For<ITypeFormats>(),
            Substitute.For<IReadModelsCompliance>(),
            Options.Create(new ChronicleOptions()),
            NullLoggerFactory.Instance);
    }
}
