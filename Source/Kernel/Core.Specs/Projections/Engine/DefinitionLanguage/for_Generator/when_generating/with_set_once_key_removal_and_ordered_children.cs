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
/// The declaration language has no syntax for these constructs yet - generating must still succeed rather than fail.
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
}
