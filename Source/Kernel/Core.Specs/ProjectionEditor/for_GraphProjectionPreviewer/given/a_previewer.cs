// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage;
using Cratis.Chronicle.Projections.Engine.Expressions;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Projections.Engine.Graphs;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Graphs;
using Cratis.Chronicle.Storage.InMemory.Sinks.Graphs;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.ProjectionEditor.for_GraphProjectionPreviewer.given;

/// <summary>
/// A graph projection previewer over a mocked event store that holds the events set on it.
/// </summary>
public class a_previewer : Specification
{
    public const string Declaration = """
        graph Organization
          node Person
            personId Uuid key
            name String
          node Group
            groupId Uuid key
            name String
          edge MEMBER_OF source Person target Group
            since DateTime
          edge PART_OF source Group target Group

        projection OrganizationGraph => graph Organization
          node Person
            from PersonRegistered key personId
              name = name
            remove with PersonRemoved key personId
          node Group
            from GroupCreated key groupId
              name = name
          edge MEMBER_OF
            from MemberAdded source personId target groupId
              since = $eventContext.occurred
            remove with MemberRemoved source personId target groupId
          edge PART_OF
            from GroupNested source childId target parentId
        """;

    public static readonly Guid Ada = new("00000000-0000-0000-0000-000000000001");
    public static readonly Guid Grace = new("00000000-0000-0000-0000-000000000002");
    public static readonly Guid Engineering = new("00000000-0000-0000-0000-000000000101");
    public static readonly Guid Platform = new("00000000-0000-0000-0000-000000000102");

    protected IGraphProjectionPreviewer _previewer;
    protected IEventSequenceStorage _eventSequenceStorage;
    protected IEnumerable<EventType> _requestedEventTypes;
    protected List<AppendedEvent> _events = [];
    protected IEnumerable<IGraphSinkFactory> _factories;

    protected static EventType EventTypeOf<T>() => new(typeof(T).Name, EventTypeGeneration.First);

    protected static AppendedEvent Event<T>(ulong sequenceNumber, Guid eventSourceId, T content)
        where T : notnull
    {
        var values = new ExpandoObject();
        var dictionary = (IDictionary<string, object?>)values;
        foreach (var property in typeof(T).GetProperties())
        {
            dictionary[char.ToLowerInvariant(property.Name[0]) + property.Name[1..]] = property.GetValue(content);
        }

        return new(
            EventContext.EmptyWithEventSourceId(eventSourceId.ToString()) with
            {
                EventType = EventTypeOf<T>(),
                SequenceNumber = sequenceNumber,
                Occurred = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(sequenceNumber)
            },
            values);
    }

    protected static PreviewGraphProjection Request(string declaration = Declaration) =>
        new("store", "namespace", declaration);

    void Establish()
    {
        var typeFormats = new TypeFormats();
        var eventValues = new EventValueProviderExpressionResolvers(typeFormats, NullLogger<EventValueProviderExpressionResolvers>.Instance);
        var properties = new ReadModelPropertyExpressionResolvers(eventValues, typeFormats, NullLogger<ReadModelPropertyExpressionResolvers>.Instance);
        var plans = new GraphProjectionPlanFactory(properties, eventValues);

        var eventTypesStorage = Substitute.For<IEventTypesStorage>();
        eventTypesStorage.GetLatestForAllEventTypes().Returns(
        [
            SchemaOf<PersonRegistered>(),
            SchemaOf<PersonRemoved>(),
            SchemaOf<GroupCreated>(),
            SchemaOf<MemberAdded>(),
            SchemaOf<MemberRemoved>(),
            SchemaOf<GroupNested>()
        ]);

        _eventSequenceStorage = Substitute.For<IEventSequenceStorage>();
        _eventSequenceStorage
            .GetEventsWithLimit(
                Arg.Any<EventSequenceNumber>(),
                Arg.Any<int>(),
                Arg.Any<EventSourceId?>(),
                Arg.Any<EventStreamType?>(),
                Arg.Any<EventStreamId?>(),
                Arg.Any<IEnumerable<EventType>?>(),
                Arg.Any<IEnumerable<Tag>?>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _requestedEventTypes = call.Arg<IEnumerable<EventType>>().ToArray();
                var cursor = Substitute.For<IEventCursor>();
                cursor.MoveNext().Returns(true, false);
                cursor.Current.Returns(_events.Where(_ => _requestedEventTypes.Contains(_.Context.EventType)).ToArray());
                return cursor;
            });

        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        namespaceStorage.GetEventSequence(Arg.Any<EventSequenceId>()).Returns(_eventSequenceStorage);
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        eventStoreStorage.EventTypes.Returns(eventTypesStorage);
        eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(namespaceStorage);
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStoreStorage);

        var compliance = Substitute.For<IEventCompliance>();
        compliance.Release(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>())
            .Returns(call => Task.FromResult(call.Arg<IEnumerable<AppendedEvent>>().ToArray()));

        var namespaceCasing = Substitute.For<INamespaceCasing>();
        namespaceCasing.Canonicalize(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>()).Returns(_ => _.Arg<EventStoreNamespaceName>());

        _factories = [new InMemoryGraphSinkFactory()];
        _previewer = new GraphProjectionPreviewer(
            storage,
            new LanguageService(new Generator(), Projections.Engine.DeclarationLanguage.CodeGeneration.given.ProjectionCodeGenerators.All()),
            plans,
            compliance,
            namespaceCasing,
            new KnownInstancesOf<ISinkFactory>([.. _factories]),
            NullLogger<GraphProjectionPreviewer>.Instance);
    }

    static EventTypeSchema SchemaOf<T>() =>
        new(EventTypeOf<T>(), EventTypeOwner.Client, EventTypeSource.Code, JsonSchema.FromType<T>());
}

public record PersonRegistered(Guid PersonId, string Name);

public record PersonRemoved(Guid PersonId);

public record GroupCreated(Guid GroupId, string Name);

public record MemberAdded(Guid PersonId, Guid GroupId);

public record MemberRemoved(Guid PersonId, Guid GroupId);

public record GroupNested(Guid ChildId, Guid ParentId);
