// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Represents the implementation of <see cref="IProjection"/>.
/// </summary>
public class Projection : IProjection, IDisposable
{
    readonly Subject<ProjectionEventContext> _subject = new();
    readonly KeyResolver? _allEventsKeyResolver;
    Dictionary<EventTypeId, KeyResolver> _keyResolverByEventTypeId = [];
    Dictionary<EventTypeId, ProjectionOperationType> _operationTypeByEventTypeId = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="Projection"/> class.
    /// </summary>
    /// <param name="eventSequenceId">The unique identifier of the event sequence.</param>
    /// <param name="identifier">The unique identifier of the projection.</param>
    /// <param name="initialModelState">The initial state to use for new model instances.</param>
    /// <param name="path">The qualified path of the projection.</param>
    /// <param name="childrenPropertyPath">The fully qualified path of the array that holds the children, if this is a child projection.</param>
    /// <param name="identifiedByProperty">The <see cref="PropertyPath"/> that identifies items in the children collection.</param>
    /// <param name="readModel">The <see cref="ReadModelDefinition"/> for the root read model.</param>
    /// <param name="readModelSchema">The target <see cref="JsonSchema"/> for the read model.</param>
    /// <param name="rewindable">Whether the projection is rewindable.</param>
    /// <param name="autoMap">Whether properties should be auto-mapped from events at the projection level.</param>
    /// <param name="noAutoMapProperties">Read model property names excluded from auto-mapping even when <paramref name="autoMap"/> is enabled.</param>
    /// <param name="childProjections">Collection of <see cref="IProjection">child projections</see>, if any.</param>
    /// <param name="subscribesToAllEvents">Whether the projection subscribes to every event type in the system, including ones not yet known when it was created.</param>
    /// <param name="allEventsKeyResolver">The <see cref="KeyResolver"/> to fall back to for an event type that has no explicit key resolver, used only when <paramref name="subscribesToAllEvents"/> is <see langword="true"/>.</param>
    /// <param name="scope">The <see cref="ProjectionScope"/> the projection materializes its read model in.</param>
    public Projection(
        EventSequenceId eventSequenceId,
        ProjectionId identifier,
        ExpandoObject initialModelState,
        ProjectionPath path,
        PropertyPath childrenPropertyPath,
        PropertyPath identifiedByProperty,
        ReadModelDefinition readModel,
        JsonSchema readModelSchema,
        bool rewindable,
        AutoMap autoMap,
        IReadOnlySet<string> noAutoMapProperties,
        IEnumerable<IProjection> childProjections,
        bool subscribesToAllEvents = false,
        KeyResolver? allEventsKeyResolver = null,
        ProjectionScope scope = ProjectionScope.Namespaced)
    {
        Scope = scope;
        EventSequenceId = eventSequenceId;
        Identifier = identifier;
        InitialModelState = initialModelState;
        ReadModel = readModel;
        TargetReadModelSchema = readModelSchema;
        IsRewindable = rewindable;
        AutoMap = autoMap;
        NoAutoMapProperties = noAutoMapProperties;
        SubscribesToAllEvents = subscribesToAllEvents;
        _allEventsKeyResolver = allEventsKeyResolver;
        Event = FilterEventTypes(_subject);
        Path = path;
        ChildrenPropertyPath = childrenPropertyPath;
        IdentifiedByProperty = identifiedByProperty;
        ChildProjections = childProjections;
    }

    /// <inheritdoc/>
    public bool SubscribesToAllEvents { get; }

    /// <inheritdoc/>
    public EventSequenceId EventSequenceId { get; }

    /// <inheritdoc/>
    public ProjectionId Identifier { get; }

    /// <inheritdoc/>
    public ExpandoObject InitialModelState { get; }

    /// <inheritdoc/>
    public ProjectionPath Path { get; }

    /// <inheritdoc/>
    public PropertyPath ChildrenPropertyPath { get; }

    /// <inheritdoc/>
    public PropertyPath IdentifiedByProperty { get; }

    /// <inheritdoc/>
    public ReadModelDefinition ReadModel { get; }

    /// <inheritdoc/>
    public JsonSchema TargetReadModelSchema { get; }

    /// <inheritdoc/>
    public bool IsRewindable { get; }

    /// <inheritdoc/>
    public ProjectionScope Scope { get; }

    /// <inheritdoc/>
    public AutoMap AutoMap { get; }

    /// <inheritdoc/>
    public IReadOnlySet<string> NoAutoMapProperties { get; }

    /// <inheritdoc/>
    public IObservable<ProjectionEventContext> Event { get; }

    /// <inheritdoc/>
    public IDictionary<EventType, ProjectionOperationType> OperationTypes { get; private set; } = new Dictionary<EventType, ProjectionOperationType>();

    /// <inheritdoc/>
    public IEnumerable<EventType> EventTypes { get; private set; } = [];

    /// <inheritdoc/>
    public IEnumerable<EventType> OwnEventTypes { get; private set; } = [];

    /// <inheritdoc/>
    public IEnumerable<IProjection> ChildProjections { get; }

    /// <inheritdoc/>
    public bool HasParent => Parent != default;

    /// <inheritdoc/>
    public bool IsEventSourceKeyed { get; private set; }

    /// <inheritdoc/>
    public IProjection? Parent { get; private set; }

    /// <inheritdoc/>
    public IEnumerable<EventTypeWithKeyResolver> EventTypesWithKeyResolver { get; private set; } = [];

    /// <summary>
    /// Gets the <see cref="CompositeDisposable"/> that holds all active pipeline subscriptions for this projection.
    /// Pass this to extension methods such as <see cref="ProjectionEventContextExtensions.Project"/> so that
    /// every subscription is tracked and explicitly disposed when the projection is disposed.
    /// </summary>
    internal CompositeDisposable Subscriptions { get; } = new();

    /// <inheritdoc/>
    public IObservable<ProjectionEventContext> FilterEventTypes(IObservable<ProjectionEventContext> observable) => observable.Where(_ => SubscribesToAllEvents || _keyResolverByEventTypeId.ContainsKey(_.Event.Context.EventType.Id));

    /// <inheritdoc/>
    public IObservable<AppendedEvent> FilterEventTypes(IObservable<AppendedEvent> observable) => observable.Where(_ => SubscribesToAllEvents || _keyResolverByEventTypeId.ContainsKey(_.Context.EventType.Id));

    /// <inheritdoc/>
    public void OnNext(ProjectionEventContext context) => _subject.OnNext(context);

    /// <inheritdoc/>
    public bool Accepts(EventType eventType) => SubscribesToAllEvents || _keyResolverByEventTypeId.ContainsKey(eventType.Id);

    /// <inheritdoc/>
    public bool HasKeyResolverFor(EventType eventType) =>
        SubscribesToAllEvents || _keyResolverByEventTypeId.ContainsKey(eventType.Id);

    /// <inheritdoc/>
    public KeyResolver GetKeyResolverFor(EventType eventType)
    {
        if (_keyResolverByEventTypeId.TryGetValue(eventType.Id, out var keyResolver))
        {
            return keyResolver;
        }

        // An event type with no explicit From/Join/RemovedWith registration reaches here only when the
        // projection subscribes to all events - the whole point being that such an event type may not have
        // existed when the projection was created, so it can never appear in the resolver map above.
        if (SubscribesToAllEvents && _allEventsKeyResolver is not null)
        {
            return _allEventsKeyResolver;
        }

        throw new MissingKeyResolverForEventType(new(eventType.Id, eventType.Generation, eventType.Tombstone));
    }

    /// <inheritdoc/>
    public ProjectionOperationType GetOperationTypeFor(EventType eventType)
    {
        if (_operationTypeByEventTypeId.TryGetValue(eventType.Id, out var operationType))
        {
            return operationType;
        }

        foreach (var child in ChildProjections)
        {
            var operation = child.GetOperationTypeFor(eventType);
            if (operation != ProjectionOperationType.None)
            {
                return operation | ProjectionOperationType.ChildrenAffected;
            }
        }

        return ProjectionOperationType.None;
    }

    /// <inheritdoc/>
    public void SetEventTypesWithKeyResolvers(
        IEnumerable<EventTypeWithKeyResolver> eventTypesWithKeyResolver,
        IEnumerable<EventType> ownEventTypes,
        IDictionary<EventType, ProjectionOperationType> operationTypes)
    {
        EventTypesWithKeyResolver = eventTypesWithKeyResolver;
        var eventTypes = eventTypesWithKeyResolver.ToArray();
        EventTypes = eventTypes.Select(_ => _.EventType).ToArray();

        // Dispatch matches purely by event type id (ignoring generation/tombstone), so the resolver map is
        // keyed by id. When several entries share an id, the first in registration order wins — the same
        // entry the previous FirstOrDefault-by-id scan returned.
        var keyResolverByEventTypeId = new Dictionary<EventTypeId, KeyResolver>();
        foreach (var entry in eventTypes)
        {
            if (!keyResolverByEventTypeId.ContainsKey(entry.EventType.Id))
            {
                keyResolverByEventTypeId[entry.EventType.Id] = entry.KeyResolver;
            }
        }

        _keyResolverByEventTypeId = keyResolverByEventTypeId;

        // A child collection routes events to a parent document, so a projection with any child projection can
        // collapse distinct event sources and must keep the coarse lock regardless of its own resolvers.
        // A projection that subscribes to all events falls back to the event-source-id key resolver for any
        // event type it has no explicit registration for (see GetKeyResolverFor), so it counts as event-source-
        // keyed on its own even with zero explicitly-registered event types.
        IsEventSourceKeyed =
            (eventTypes.Length > 0 || SubscribesToAllEvents) &&
            !ChildProjections.Any() &&
            eventTypes.All(_ => _.ResolvesToEventSourceId);

        OwnEventTypes = ownEventTypes;
        OperationTypes = operationTypes;

        var operationTypeByEventTypeId = new Dictionary<EventTypeId, ProjectionOperationType>();
        foreach (var (eventType, operationType) in operationTypes)
        {
            if (!operationTypeByEventTypeId.ContainsKey(eventType.Id))
            {
                operationTypeByEventTypeId[eventType.Id] = operationType;
            }
        }

        _operationTypeByEventTypeId = operationTypeByEventTypeId;
    }

    /// <inheritdoc/>
    public void SetParent(IProjection projection) => Parent = projection;

    /// <inheritdoc/>
    public void Dispose()
    {
        _subject.OnCompleted();
        Subscriptions.Dispose();
        _subject.Dispose();
    }
}
