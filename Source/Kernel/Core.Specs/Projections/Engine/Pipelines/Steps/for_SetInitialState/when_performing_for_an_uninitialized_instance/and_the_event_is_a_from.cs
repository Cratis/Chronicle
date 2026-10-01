// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SetInitialState.when_performing_for_an_uninitialized_instance;

public class and_the_event_is_a_from : given.a_set_initial_state_step_with_an_uninitialized_instance
{
    ProjectionEventContext _context;

    void Establish() => _context = CreateContext(ProjectionOperationType.From);

    async Task Because() => _context = await _step.Perform(_projection, _context);

    [Fact] void should_record_the_initial_values_as_changes() => ChangedProperties(_context).ShouldContain((PropertyPath)"status");
    [Fact] void should_not_record_the_key_as_a_change() => ChangedProperties(_context).ShouldNotContain((PropertyPath)"id");
    [Fact] void should_not_record_the_children_as_a_change() => ChangedProperties(_context).ShouldNotContain((PropertyPath)"items");
    [Fact] void should_mark_the_instance_as_initialized() => ChangedProperties(_context).ShouldContain((PropertyPath)"__initialized");
}
