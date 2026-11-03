// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Storage.Sinks;
using Orleans.TestKit;

using EngineProjection = Cratis.Chronicle.Projections.Engine.IProjection;
using EngineProjectionsManager = Cratis.Chronicle.Projections.Engine.IProjectionsManager;

namespace Cratis.Chronicle.Observation.for_PartitionRebuildAdmission.given;

public class an_admitted_rebuilder : an_admission_check
{
    protected ProjectionPartitionRebuilder _rebuilder;
    protected bool _wasAdmitted;

    async Task Establish()
    {
        _wasAdmitted = await _admission.IsAdmitted(_request);
        var silo = new TestKitSilo();
        silo.AddService(_storage);
        silo.AddService<IPartitionRebuildAdmission>(_admission);
        var projection = Substitute.For<EngineProjection>();
        projection.ReadModel.Returns(_readModel);
        projection.EventTypes.Returns(_request.EventTypes);
        var projections = Substitute.For<EngineProjectionsManager>();
        projections.GetCurrent(_key.EventStore, _key.Namespace, _key.ObserverId).Returns(projection);
        silo.AddService(projections);
        var pipelines = Substitute.For<IProjectionPipelineManager>();
        pipelines.CreateIsolated(_key.EventStore, _key.Namespace, projection, Arg.Any<ISink>()).Returns(Substitute.For<IProjectionPipeline>());
        silo.AddService(pipelines);
        _rebuilder = await silo.CreateGrainAsync<ProjectionPartitionRebuilder>(Guid.NewGuid().ToString());
        await _rebuilder.Begin(_key, _request.Key);
    }
}
