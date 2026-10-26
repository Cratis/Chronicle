// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SetInitialState.when_performing_for_a_new_instance;

public class and_the_root_is_waiting_for_its_own_event : given.a_set_initial_state_step
{
    ProjectionEventContext _context;

    void Establish()
    {
        _projection.ChildProjections.Returns([Substitute.For<IProjection>()]);
        var created = new EventType("RootCreated", EventTypeGeneration.First);
        _projection.OwnEventTypes.Returns([created]);
        _projection.OperationTypes.Returns(new Dictionary<EventType, ProjectionOperationType> { [created] = ProjectionOperationType.From });
        _context = CreateContext(ProjectionOperationType.From | ProjectionOperationType.ChildrenAffected);
    }

    async Task Because() => _context = await _step.Perform(_projection, _context);

    [Fact] void should_not_need_the_initial_state() => _context.NeedsInitialState.ShouldBeFalse();
    [Fact] void should_leave_the_root_uninitialized() => ((IDictionary<string, object?>)_context.Changeset.InitialState)[WellKnownProperties.ReadModelInstanceInitialized].ShouldEqual(false);
}
