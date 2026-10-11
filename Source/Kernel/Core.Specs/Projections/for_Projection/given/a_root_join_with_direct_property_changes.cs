// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.for_Projection.given;

public class a_root_join_with_direct_property_changes : a_projection_sequence_ending_after_removal
{
    void Establish()
    {
        _rootProjection.ChildProjections.Returns([]);
        _rootProjection.GetOperationTypeFor(Arg.Any<EventType>()).Returns(call =>
            call.Arg<EventType>() == RootJoinEvent.Context.EventType ? ProjectionOperationType.Join : ProjectionOperationType.From);
        ProjectRootWith(context =>
        {
            if (context.Event.Context.EventType == CreatedEvent.Context.EventType)
            {
                context.Changeset.Add(new PropertiesChanged<ExpandoObject>(context.Changeset.CurrentState,
                [
                    new PropertyDifference("Name", null, "Original"),
                    new PropertyDifference("CustomerId", null, "the-customer")
                ]));
                return;
            }

            context.Changeset.Add(new PropertiesChanged<ExpandoObject>(context.Changeset.CurrentState,
            [
                new PropertyDifference("Name", "Original", "Not a keyed update")
            ]));
            context.Changeset.Add(new Joined(
                context.Changeset.CurrentState,
                "the-customer",
                new PropertyPath("CustomerId"),
                ArrayIndexers.NoIndexers,
                [new PropertiesChanged<ExpandoObject>(context.Changeset.CurrentState, [new PropertyDifference("CustomerName", null, "Ada")])]));
        });
    }
}
