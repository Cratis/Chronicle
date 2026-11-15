// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionDefinitionInitialization;

public class when_comparing_metadata_to_compiled_initialization : given.a_definition_with_children
{
    readonly Dictionary<string, (ProjectionDefinition Definition, bool ChildOnly)> _scenarios = [];
    readonly Dictionary<string, bool> _matches = [];

    void Establish()
    {
        _scenarios["children"] = (_definition, true);
        _scenarios["no children"] = (_definition with { Children = new Dictionary<PropertyPath, ChildrenDefinition>() }, false);
        _scenarios["root from"] = (_definition with { From = new Dictionary<EventType, FromDefinition> { [Event] = From } }, false);
        _scenarios["root join"] = (_definition with { Join = new Dictionary<EventType, JoinDefinition> { [Event] = Join } }, true);
        _scenarios["from and join"] = (_definition with
        {
            From = new Dictionary<EventType, FromDefinition> { [Event] = From },
            Join = new Dictionary<EventType, JoinDefinition> { [Event] = Join }
        }, true);
        _scenarios["derivative"] = (_definition with { FromDerivatives = [new FromDerivatives([Event], From)] }, false);
        _scenarios["event property"] = (_definition with { FromEventProperty = new(Event, "id") }, false);
        _scenarios["all events"] = (_definition with { SubscribesToAllEvents = true }, false);
        _scenarios["nested from"] = (WithNested(Child() with { From = new Dictionary<EventType, FromDefinition> { [Event] = From } }), false);
        _scenarios["nested removal"] = (WithNested(Child() with { RemovedWith = new Dictionary<EventType, RemovedWithDefinition> { [Event] = new(PropertyExpression.NotSet, null) } }), false);
        _scenarios["descendant from"] = (_definition with
        {
            Children = new Dictionary<PropertyPath, ChildrenDefinition>
            {
                ["entries"] = Child() with { From = new Dictionary<EventType, FromDefinition> { [Event] = From } }
            }
        }, true);
    }

    async Task Because()
    {
        foreach (var (name, scenario) in _scenarios)
        {
            _matches[name] = await MatchesCompiledInitialization(scenario.Definition) &&
                ProjectionDefinitionInitialization.IsChildOnly(scenario.Definition) == scenario.ChildOnly;
        }
    }

    ProjectionDefinition WithNested(ChildrenDefinition nested) => _definition with { Nested = new Dictionary<PropertyPath, ChildrenDefinition> { ["details"] = nested } };

    [Fact] void should_not_treat_graph_projections_as_child_only_roots() => ProjectionDefinitionInitialization.IsChildOnly(_definition with { Graph = Concepts.Projections.Graphs.GraphProjectionDefinition.Empty }).ShouldBeFalse();
    [Fact] void should_match_for_child_only_roots() => _matches["children"].ShouldBeTrue();
    [Fact] void should_match_without_children() => _matches["no children"].ShouldBeTrue();
    [Fact] void should_match_for_root_from() => _matches["root from"].ShouldBeTrue();
    [Fact] void should_match_for_join_only_roots() => _matches["root join"].ShouldBeTrue();
    [Fact] void should_match_for_overlapping_from_and_join() => _matches["from and join"].ShouldBeTrue();
    [Fact] void should_match_for_derivative_creators() => _matches["derivative"].ShouldBeTrue();
    [Fact] void should_match_for_event_property_creators() => _matches["event property"].ShouldBeTrue();
    [Fact] void should_match_for_all_event_subscriptions() => _matches["all events"].ShouldBeTrue();
    [Fact] void should_match_for_nested_creators() => _matches["nested from"].ShouldBeTrue();
    [Fact] void should_match_for_nested_removal_registrations() => _matches["nested removal"].ShouldBeTrue();
    [Fact] void should_not_treat_descendant_creators_as_root_creators() => _matches["descendant from"].ShouldBeTrue();
}
