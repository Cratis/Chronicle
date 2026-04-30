// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
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

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers.when_identifying_read_model_key_for_nested_event_within_grandchild_projection.given;

/// <summary>
/// Sets up a three-level projection hierarchy: Root → Feature (child) → Slice (grandchild).
/// A nested command event is appended to the same event source as the slice.
/// The parent key for the nested event defaults to the event source (SliceKey) — this is the
/// broken case where FeatureItem events use FeatureKey, so the standard parent lookup fails.
/// </summary>
public class a_three_level_hierarchy : Specification
{
    protected const string RootKey = "root-key";
    protected const string FeatureKey = "feature-key";
    protected const string SliceKey = "slice-key";

    protected static readonly EventType RootEventType = new("root-event-type", 1);
    protected static readonly EventType FeatureAddedEventType = new("feature-added-event-type", 1);
    protected static readonly EventType SliceAddedEventType = new("slice-added-event-type", 1);
    protected static readonly EventType NestedCommandEventType = new("nested-command-event-type", 1);

    protected AppendedEvent ModuleAddedEvent;
    protected AppendedEvent FeatureAddedEvent;
    protected AppendedEvent SliceAddedEvent;
    protected AppendedEvent NestedCommandEvent;

    protected IProjection RootProjection;
    protected IProjection FeatureProjection;
    protected IProjection SliceProjection;

    protected IEventSequenceStorage Storage;
    protected ISink Sink;
    protected KeyResolvers KeyResolvers;

    void Establish()
    {
        Storage = Substitute.For<IEventSequenceStorage>();
        Sink = Substitute.For<ISink>();
        KeyResolvers = new KeyResolvers(NullLogger<KeyResolvers>.Instance);

        ModuleAddedEvent = CreateEvent(0, RootEventType, RootKey, new ExpandoObject());
        FeatureAddedEvent = CreateEvent(1, FeatureAddedEventType, FeatureKey, new { moduleId = RootKey, featureId = FeatureKey }.AsExpandoObject());
        SliceAddedEvent = CreateEvent(2, SliceAddedEventType, SliceKey, new { featureId = FeatureKey, sliceId = SliceKey }.AsExpandoObject());
        NestedCommandEvent = CreateEvent(3, NestedCommandEventType, SliceKey, new { sliceId = SliceKey }.AsExpandoObject());

        RootProjection = Substitute.For<IProjection>();
        RootProjection.HasParent.Returns(false);
        RootProjection.Parent.Returns((IProjection)null!);
        RootProjection.ChildrenPropertyPath.Returns(PropertyPath.NotSet);
        RootProjection.IdentifiedByProperty.Returns((PropertyPath)"id");
        RootProjection.Path.Returns((ProjectionPath)"root");
        RootProjection.OwnEventTypes.Returns([RootEventType]);
        RootProjection.EventTypes.Returns([RootEventType]);
        RootProjection.GetKeyResolverFor(RootEventType).Returns((_, __, ___) =>
            Task.FromResult(KeyResolverResult.Resolved(new Key(RootKey, ArrayIndexers.NoIndexers))));

        FeatureProjection = Substitute.For<IProjection>();
        FeatureProjection.HasParent.Returns(true);
        FeatureProjection.Parent.Returns(RootProjection);
        FeatureProjection.ChildrenPropertyPath.Returns((PropertyPath)"features");
        FeatureProjection.IdentifiedByProperty.Returns((PropertyPath)"featureId");
        FeatureProjection.Path.Returns((ProjectionPath)"features");
        FeatureProjection.OwnEventTypes.Returns([FeatureAddedEventType]);
        FeatureProjection.EventTypes.Returns([FeatureAddedEventType, SliceAddedEventType, NestedCommandEventType]);

        SliceProjection = Substitute.For<IProjection>();
        SliceProjection.HasParent.Returns(true);
        SliceProjection.Parent.Returns(FeatureProjection);
        SliceProjection.ChildrenPropertyPath.Returns((PropertyPath)"slices");
        SliceProjection.IdentifiedByProperty.Returns((PropertyPath)"sliceId");
        SliceProjection.Path.Returns((ProjectionPath)"slices");
        SliceProjection.OwnEventTypes.Returns([SliceAddedEventType, NestedCommandEventType]);
        SliceProjection.EventTypes.Returns([SliceAddedEventType, NestedCommandEventType]);

        // Wire up the key resolvers that the real resolution chain will call.
        // NSubstitute requires that return values are computed before passing to Returns()
        // to avoid "CouldNotSetReturnDueToNoLastCallException" when the factory methods
        // internally read properties from substitute objects.
        var featureAddedKeyResolver = KeyResolvers.FromParentHierarchy(
            FeatureProjection,
            KeyResolvers.FromEventSourceId,
            KeyResolvers.FromEventValueProvider(EventValueProviders.EventContent("moduleId")),
            "featureId");
        FeatureProjection.GetKeyResolverFor(FeatureAddedEventType).Returns(featureAddedKeyResolver);

        var sliceAddedKeyResolver = KeyResolvers.FromParentHierarchy(
            SliceProjection,
            KeyResolvers.FromEventSourceId,
            KeyResolvers.FromEventValueProvider(EventValueProviders.EventContent("featureId")),
            "sliceId");
        FeatureProjection.GetKeyResolverFor(SliceAddedEventType).Returns(sliceAddedKeyResolver);

        // The parent event lookup fails because FeatureAdded events have FeatureKey as event source,
        // not SliceKey. This is the trigger for TryResolveViaChildCreationEvent.
        Storage.TryGetLastInstanceOfAny(SliceKey, Arg.Any<IEnumerable<EventTypeId>>())
            .Returns(Option<AppendedEvent>.None());
    }

    protected static AppendedEvent CreateEvent(ulong sequenceNumber, EventType eventType, EventSourceId eventSourceId, ExpandoObject content) =>
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
            content);

    protected IEventCursor CreateCursorWith(params AppendedEvent[] events)
    {
        var cursor = Substitute.For<IEventCursor>();
        cursor.Current.Returns(events);
        var callCount = 0;
        cursor.MoveNext().Returns(_ => Task.FromResult(callCount++ == 0));
        return cursor;
    }
}
