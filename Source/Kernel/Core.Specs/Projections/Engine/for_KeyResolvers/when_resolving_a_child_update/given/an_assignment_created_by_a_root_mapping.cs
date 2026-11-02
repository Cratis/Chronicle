// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Monads;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers.when_resolving_a_child_update.given;

/// <summary>
/// Sets up assignments created on their own event sources by an event that also maps
/// the root under its project asset identity. Only creation-event lookup can locate the root.
/// </summary>
public class an_assignment_created_by_a_root_mapping : Specification
{
    protected const string ProjectAssetKey = "project-asset";
    protected const string AssignmentKey = "assignment";

    protected static readonly EventType AssignedEventType = new("asset-parsell-assigned", 1);
    protected static readonly EventType WithheldEventType = new("asset-parsell-assignment-withheld", 1);
    protected static readonly EventType EndedEventType = new("asset-parsell-assignment-ended", 1);

    protected AppendedEvent AssignedEvent;
    protected AppendedEvent WithheldEvent;
    protected AppendedEvent EndedEvent;
    protected IEventSequenceStorage Storage;
    protected ISink Sink;
    protected KeyResolver Resolver;

    void Establish()
    {
        Storage = Substitute.For<IEventSequenceStorage>();
        Sink = Substitute.For<ISink>();
        Sink.TryFindRootKeyByChildValue(Arg.Any<PropertyPath>(), Arg.Any<object>())
            .Returns(Option<Key>.None());
        var keyResolvers = new KeyResolvers(NullLogger<KeyResolvers>.Instance);

        AssignedEvent = CreateEvent(1, AssignedEventType, AssignmentKey);
        WithheldEvent = CreateEvent(3, WithheldEventType, AssignmentKey);
        EndedEvent = CreateEvent(4, EndedEventType, AssignmentKey);

        var rootProjection = Substitute.For<IProjection>();
        rootProjection.HasParent.Returns(false);
        rootProjection.ChildrenPropertyPath.Returns(PropertyPath.NotSet);
        rootProjection.Path.Returns((ProjectionPath)"root");
        rootProjection.OwnEventTypes.Returns([AssignedEventType]);
        rootProjection.EventTypes.Returns([AssignedEventType]);
        var rootResolver = keyResolvers.FromEventValueProvider(EventValueProviders.EventContent("projectAssetId"));
        rootProjection.GetKeyResolverFor(AssignedEventType).Returns(rootResolver);

        var assignmentProjection = Substitute.For<IProjection>();
        assignmentProjection.HasParent.Returns(true);
        assignmentProjection.Parent.Returns(rootProjection);
        assignmentProjection.ChildrenPropertyPath.Returns((PropertyPath)"assignments");
        assignmentProjection.IdentifiedByProperty.Returns((PropertyPath)"assignmentId");
        assignmentProjection.Path.Returns((ProjectionPath)"assignments");
        assignmentProjection.OwnEventTypes.Returns([AssignedEventType, WithheldEventType, EndedEventType]);
        assignmentProjection.EventTypes.Returns([AssignedEventType, WithheldEventType, EndedEventType]);

        Resolver = keyResolvers.FromParentHierarchy(
            assignmentProjection,
            keyResolvers.FromEventSourceId,
            keyResolvers.FromEventValueProvider(EventValueProviders.EventContent("projectAssetId")),
            "assignmentId");
        assignmentProjection.GetKeyResolverFor(AssignedEventType).Returns(Resolver);

        // The root's key is not the creation event's source, so parent history under that key is empty.
        Storage.WithParentHistory(ProjectAssetKey);
        WithCreationEvent(AssignedEvent);
    }

    protected void WithCreationEvent(AppendedEvent creationEvent)
    {
        Storage.GetHeadSequenceNumber(Arg.Any<IEnumerable<EventType>>(), creationEvent.Context.EventSourceId)
            .Returns(creationEvent.Context.SequenceNumber);
        Storage.GetRange(
                Arg.Any<EventSequenceNumber>(),
                Arg.Any<EventSequenceNumber>(),
                creationEvent.Context.EventSourceId,
                Arg.Any<IEnumerable<EventType>>(),
                Arg.Any<IEnumerable<Tag>?>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                var cursor = Substitute.For<IEventCursor>();
                cursor.Current.Returns([creationEvent]);
                var callCount = 0;
                cursor.MoveNext().Returns(__ => Task.FromResult(callCount++ == 0));
                return cursor;
            });
    }

    protected static AppendedEvent CreateEvent(ulong sequenceNumber, EventType eventType, EventSourceId eventSourceId) =>
        new(
            new(
                eventType,
                EventSourceType.Default,
                eventSourceId,
                EventStreamType.All,
                EventStreamId.Default,
                sequenceNumber,
                DateTimeOffset.UtcNow,
                "123b8935-a1a4-410d-aace-e340d48f0aa0",
                "41f18595-4748-4b01-88f7-4c0d0907aa90",
                CorrelationId.New(),
                [],
                Identity.System,
                [],
                EventHash.NotSet),
            new { projectAssetId = ProjectAssetKey }.AsExpandoObject());
}
