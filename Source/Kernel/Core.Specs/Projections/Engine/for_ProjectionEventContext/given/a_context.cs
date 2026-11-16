// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionEventContext.given;

public class a_context : Specification
{
    protected ProjectionEventContext _context;

    protected ProjectionFuture FutureFor(EventType eventType) => new(
        Guid.NewGuid(),
        "projection",
        _context.Event with { Context = _context.Event.Context with { EventType = eventType } },
        "parents",
        "parents.children",
        "id",
        "id",
        _context.Key,
        DateTimeOffset.UnixEpoch);

    protected IChangeset<AppendedEvent, ExpandoObject> Changeset() => Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();

    void Establish() => _context = ProjectionEventContext.Empty(Substitute.For<IObjectComparer>(), AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(new EventType("event", 1), 42UL));
}
