// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SetInitialState.when_performing_for_an_uninitialized_instance;

public class and_the_root_has_only_children : given.a_set_initial_state_step_with_an_uninitialized_instance
{
    ProjectionEventContext _context;

    void Establish()
    {
        var child = Substitute.For<IProjection>();
        child.ChildrenPropertyPath.Returns(new PropertyPath("Children"));
        _projection.ChildProjections.Returns([child]);
        _context = CreateContext(ProjectionOperationType.From | ProjectionOperationType.ChildrenAffected);
    }

    async Task Because() => _context = await _step.Perform(_projection, _context);

    [Fact] void should_initialize_the_existing_root() => ((IDictionary<string, object?>)_context.Changeset.InitialState)[WellKnownProperties.ReadModelInstanceInitialized].ShouldEqual(true);
    [Fact] void should_record_the_initialization() => ChangedProperties(_context).ShouldContain(new PropertyPath(WellKnownProperties.ReadModelInstanceInitialized));
}
