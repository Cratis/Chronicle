// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionEventContextExtensions.when_resolving_join;

public class and_upstream_completes : Specification
{
    Subject<ProjectionEventContext> _subject;
    IEventSequenceStorage _eventSequenceStorage;
    bool _completed;

    void Establish()
    {
        _subject = new Subject<ProjectionEventContext>();
        _eventSequenceStorage = Substitute.For<IEventSequenceStorage>();
        _eventSequenceStorage
            .TryGetLastEventBefore(Arg.Any<EventTypeId>(), Arg.Any<EventSourceId>(), Arg.Any<EventSequenceNumber>())
            .Returns(Task.FromResult(Catch<Option<AppendedEvent>>.Success(Option<AppendedEvent>.None())));

        _subject
            .ResolveJoin(_eventSequenceStorage, new EventType(new EventTypeId("e1"), 1), PropertyPath.Root, Substitute.For<ILogger>())
            .Subscribe(_ => { }, () => _completed = true);
    }

    void Because() => _subject.OnCompleted();

    [Fact] void should_propagate_completion_downstream() => _completed.ShouldBeTrue();
}
