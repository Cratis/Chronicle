// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_SetInitialState.when_performing_for_an_uninitialized_instance;

public class and_a_placeholder_is_filled_with_a_nested_initial_value : given.a_set_initial_state_step_with_an_uninitialized_instance
{
    ProjectionEventContext _context;
    ExpandoObject _initialNested;

    void Establish()
    {
        _initialNested = new ExpandoObject();
        ((IDictionary<string, object?>)_initialNested)["inner"] = "value";
        ((IDictionary<string, object?>)_projection.InitialModelState)["details"] = _initialNested;
        _context = CreateContext(ProjectionOperationType.From);
    }

    async Task Because() => _context = await _step.Perform(_projection, _context);

    [Fact] void should_not_share_the_nested_value_with_the_state() => ((IDictionary<string, object?>)_context.Changeset.CurrentState)["details"].ShouldNotBeSame(_initialNested);
    [Fact] void should_copy_the_nested_value_to_the_state() => ((IDictionary<string, object?>)((IDictionary<string, object?>)_context.Changeset.CurrentState)["details"]!)["inner"].ShouldEqual("value");
    [Fact] void should_not_share_the_nested_value_with_the_recorded_change() => RecordedDetails().Changed.ShouldNotBeSame(_initialNested);

    PropertyDifference RecordedDetails() =>
        _context.Changeset.Changes
            .OfType<PropertiesChanged<ExpandoObject>>()
            .SelectMany(_ => _.Differences)
            .Single(_ => _.PropertyPath == "details");
}
