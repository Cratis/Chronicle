// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SetInitialState.when_performing_for_an_uninitialized_instance;

public class and_the_sink_does_not_store_whether_the_instance_is_initialized : given.a_set_initial_state_step_with_an_uninitialized_instance
{
    ProjectionEventContext _context;

    void Establish()
    {
        var stored = new ExpandoObject();
        ((IDictionary<string, object?>)stored)["id"] = "the-key";
        _sink.FindOrDefault(Arg.Any<Key>()).Returns(stored);
        _context = CreateContext(ProjectionOperationType.From | ProjectionOperationType.ChildrenAffected);
    }

    async Task Because() => _context = await _step.Perform(_projection, _context);

    [Fact] void should_mark_the_instance_as_initialized() => ChangedProperties(_context).ShouldContain((PropertyPath)WellKnownProperties.ReadModelInstanceInitialized);
    [Fact] void should_not_record_the_initial_values_as_changes() => ChangedProperties(_context).ShouldNotContain((PropertyPath)"status");
}
