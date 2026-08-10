// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_for_a_single_read_model;

public class and_the_root_is_removed : given.a_projection_grain_with_a_child_projection
{
    readonly EventType _createdEventType = new("Created", EventTypeGeneration.First);
    readonly EventType _removedEventType = new("Removed", EventTypeGeneration.First);
    ExpandoObject _result;

    void Establish()
    {
        ProjectRootWith(context =>
        {
            if (context.Event.Context.EventType == _removedEventType)
            {
                context.Changeset.Remove();
                return;
            }

            context.Changeset.Add(new PropertiesChanged<ExpandoObject>(context.Changeset.CurrentState,
            [
                new PropertyDifference("Name", null, "The model")
            ]));
        });
    }

    async Task Because() => _result = await ProcessForSingleReadModel(
        AppendedEvent.EmptyWithEventType(_createdEventType),
        AppendedEvent.EmptyWithEventType(_removedEventType));

    [Fact] void should_not_return_a_read_model() => _result.ShouldBeEmpty();
}
