// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionsManager.given;

public class a_manager_with_shared_definitions : a_projections_manager
{
    protected ProjectionDefinition _stored;
    protected IProjection _result;

    void Establish()
    {
        _stored = _firstDefinition;
        _storage.GetEventStore(_eventStore).Projections.Has(_firstDefinition.Identifier).Returns(true);
        _storage.GetEventStore(_eventStore).Projections.Get(_firstDefinition.Identifier).Returns(_ => _stored);
        var readModel = Substitute.For<IReadModel>();
        readModel.GetDefinition().Returns(_firstReadModelDefinition);
        _grainFactory.GetGrain<IReadModel>(new ReadModelGrainKey(_firstDefinition.ReadModel, _eventStore)).Returns(readModel);
        _projectionFactory.Create(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ReadModelDefinition>(), Arg.Any<IEnumerable<EventTypeSchema>>())
            .Returns(call =>
            {
                var projection = Substitute.For<IProjection>();
                projection.DefinitionFingerprint.Returns(ProjectionFactory.DefinitionFingerprintFor(call.Arg<ProjectionDefinition>()));
                return projection;
            });
    }
}
