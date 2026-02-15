// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Subjects;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionExtensions.given;

public class an_observable_and_event_setup : Specification
{
    protected Subject<ProjectionEventContext> _observable;
    protected List<ProjectionEventContext> _received = [];
    protected ProjectionEventContext _eventContext;
    protected AppendedEvent _event;
    protected IChangeset<AppendedEvent, ExpandoObject> _changeset;
    protected ExpandoObject _initialState;

    void Establish()
    {
        _observable = new();

        _event = new(
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

        _initialState = new();
        _changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        _changeset.InitialState.Returns(_initialState);
        _changeset.Incoming.Returns(_event);

        _eventContext = new(new(_event.Context.EventSourceId, ArrayIndexers.NoIndexers), _event, _changeset, ProjectionOperationType.From, false);
    }
}
