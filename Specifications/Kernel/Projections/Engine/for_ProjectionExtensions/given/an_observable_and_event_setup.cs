// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Subjects;
using Aksio.Cratis.Changes;

namespace Aksio.Cratis.Events.Projections.for_ProjectionExtensions.given
{
    public class an_observable_and_event_setup : Specification
    {
        protected Subject<EventContext> observable;
        protected List<EventContext> received = new();
        protected EventContext event_context;
        protected Event @event;
        protected Mock<IChangeset<Event, ExpandoObject>> changeset;
        protected ExpandoObject initial_state;

        void Establish()
        {
            observable = new();

            @event = new(
                    1,
                    new EventType("98949b84-ee1b-48d5-a486-c5adf0332028", 1),
                    DateTimeOffset.UtcNow,
                    "14b33c19-1311-4825-93f9-bedab9e7d5ee",
                    new ExpandoObject());

            initial_state = new();
            changeset = new();
            changeset.SetupGet(_ => _.InitialState).Returns(initial_state);
            changeset.SetupGet(_ => _.Incoming).Returns(@event);

            event_context = new(@event, changeset.Object);
        }
    }
}
