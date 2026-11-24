// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_Generator.when_generating;

/// <summary>
/// Generation preserves set-once mappings, dictionary removal and child ordering.
/// </summary>
public class with_set_once_key_removal_and_ordered_children : given.a_generator
{
    Exception? _error;

    void Establish()
    {
        var readModelDefinition = CreateReadModelDefinition("Example.ReadModels.Model", schemaTitle: null);
        var slotAssigned = new EventType("SlotAssigned", 1);
        var entryAdded = new EventType("EntryAdded", 1);
        var children = new ChildrenDefinition(
            new PropertyPath("entryId"),
            new Dictionary<EventType, FromDefinition>
            {
                [entryAdded] = new(new Dictionary<PropertyPath, string> { [new PropertyPath("name")] = "name" }, new PropertyExpression("entryId"), null)
            },
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), true),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>(),
            Ordering: new ChildrenOrdering(ChildrenPlacement.Sorted, new PropertyPath("name"), SortDirection.Descending));

        var projectionDefinition = CreateProjectionDefinition("Model", readModelDefinition.Identifier) with
        {
            From = new Dictionary<EventType, FromDefinition>
            {
                [slotAssigned] = new(
                    new Dictionary<PropertyPath, string>
                    {
                        [new PropertyPath("slots.$eventContent.slot")] = "seriesId",
                        [new PropertyPath("dropped.$eventContent.slot")] = WellKnownExpressions.Remove,
                        [new PropertyPath("createdBy")] = $"{WellKnownExpressions.SetOnce}($causedBy(subject))",
                        [new PropertyPath("createdAt")] = $"{WellKnownExpressions.SetOnce}($eventContext(occurred))"
                    },
                    new PropertyExpression(WellKnownExpressions.EventSourceId),
                    null)
            },
            Children = new Dictionary<PropertyPath, ChildrenDefinition> { [new PropertyPath("entries")] = children }
        };

        _error = Catch.Exception(() => _result = _generator.Generate(projectionDefinition, readModelDefinition));
    }

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_generate_a_declaration() => _result.ShouldNotBeNull();
    [Fact] void should_keep_the_event_the_new_constructs_are_declared_on() => _result.ShouldContain("from SlotAssigned");
    [Fact] void should_keep_the_ordered_children() => _result.ShouldContain("children entries identified by entryId");
    [Fact] void should_keep_the_mappings_of_the_ordered_children() => _result.ShouldContain("from EntryAdded");
    [Fact] void should_keep_dictionary_removal() => _result.ShouldContain("remove dropped.$eventContent.slot");
    [Fact] void should_keep_set_once_identity() => _result.ShouldContain("set once createdBy = $causedBy.subject");
    [Fact] void should_keep_set_once_context() => _result.ShouldContain("set once createdAt = $eventContext.occurred");
    [Fact] void should_keep_descending_order() => _result.ShouldContain("sort by name descending");
}
