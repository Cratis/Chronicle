// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SetInitialState.when_performing_for_a_new_instance;

public class and_the_root_has_only_children : given.a_set_initial_state_step
{
    ProjectionEventContext _context;

    void Establish()
    {
        _projection.ChildProjections.Returns([Substitute.For<IProjection>()]);
        var removed = new EventType("RootRemoved", EventTypeGeneration.First);
        _projection.OwnEventTypes.Returns([removed]);
        _projection.OperationTypes.Returns(new Dictionary<EventType, ProjectionOperationType> { [removed] = ProjectionOperationType.Remove });
        _context = CreateContext(ProjectionOperationType.From | ProjectionOperationType.ChildrenAffected);
    }

    async Task Because() => _context = await _step.Perform(_projection, _context);

    [Fact] void should_need_the_initial_state() => _context.NeedsInitialState.ShouldBeTrue();
    [Fact] void should_initialize_the_root() => ((IDictionary<string, object?>)_context.Changeset.InitialState)[WellKnownProperties.ReadModelInstanceInitialized].ShouldEqual(true);
}
