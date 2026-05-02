// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Subjects;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionEventContextExtensions.given;

public class a_projection_event_context_observable : Specification
{
    protected Subject<ProjectionEventContext> _subject;
    protected ProjectionEventContext _eventContext;

    void Establish()
    {
        _subject = new Subject<ProjectionEventContext>();

        var @event = new AppendedEvent(
            new(
                new("02405794-91e7-4e4f-8ad1-f043070ca297", 1),
                EventSourceType.Default,
                "2f005aaf-2f4e-4a47-92ea-63687ef74bd4",
                EventStreamType.All,
                EventStreamId.Default,
                1,
                DateTimeOffset.UtcNow,
                "123b8935-a1a4-410d-aace-e340d48f0aa0",
                "41f18595-4748-4b01-88f7-4c0d0907aa90",
                CorrelationId.New(),
                [],
                Identity.System,
                [],
                EventHash.NotSet),
            new ExpandoObject());

        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.CurrentState.Returns(new ExpandoObject());
        changeset.InitialState.Returns(new ExpandoObject());
        changeset.Incoming.Returns(@event);
        changeset.Join(Arg.Any<PropertyPath>(), Arg.Any<object>(), Arg.Any<ArrayIndexers>())
            .Returns(changeset);

        _eventContext = new(
            new(@event.Context.EventSourceId, ArrayIndexers.NoIndexers),
            @event,
            changeset,
            ProjectionOperationType.From,
            false);
    }
}
