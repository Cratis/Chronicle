// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Subjects;
using System.Text.Json.Nodes;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Events.Store;

namespace Aksio.Cratis.Events.Projections.for_ProjectionExtensions.given
{
    public class an_observable_and_event_setup : Specification
    {
        protected Subject<ProjectionEventContext> observable;
        protected List<ProjectionEventContext> received = new();
        protected ProjectionEventContext event_context;
        protected AppendedEvent @event;
        protected Mock<IChangeset<AppendedEvent, ExpandoObject>> changeset;
        protected ExpandoObject initial_state;

        void Establish()
        {
            observable = new();

            @event = new(new(1, new("02405794-91e7-4e4f-8ad1-f043070ca297", 1)), new("2f005aaf-2f4e-4a47-92ea-63687ef74bd4", DateTimeOffset.UtcNow), new JsonObject());
            initial_state = new();
            changeset = new();
            changeset.SetupGet(_ => _.InitialState).Returns(initial_state);
            changeset.SetupGet(_ => _.Incoming).Returns(@event);

            event_context = new(@event, changeset.Object);
        }
    }
}
