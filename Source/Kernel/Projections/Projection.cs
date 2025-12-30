// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;
using NJsonSchema;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents the implementation of <see cref="IProjection"/>.
/// </summary>
public class Projection : IProjection, IDisposable
{
    readonly Subject<ProjectionEventContext> _subject = new();
    Dictionary<EventType, KeyResolver> _eventTypesToKeyResolver = [];

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
    /// <param name="childProjections">Collection of <see cref="IProjection">child projections</see>, if any.</param>
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
        IEnumerable<IProjection> childProjections)
    {
        EventSequenceId = eventSequenceId;
        Identifier = identifier;
        InitialModelState = initialModelState;
        ReadModel = readModel;
        TargetReadModelSchema = readModelSchema;
        IsRewindable = rewindable;
        Event = FilterEventTypes(_subject);
        Path = path;
        ChildrenPropertyPath = childrenPropertyPath;
        IdentifiedByProperty = identifiedByProperty;
        ChildProjections = childProjections;
    }

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
    public IProjection? Parent { get; private set; }

    /// <inheritdoc/>
    public IEnumerable<EventTypeWithKeyResolver> EventTypesWithKeyResolver { get; private set; } = [];

    /// <inheritdoc/>
    public IObservable<ProjectionEventContext> FilterEventTypes(IObservable<ProjectionEventContext> observable) => observable.Where(_ => EventTypes.Any(et => et.Id == _.Event.Context.EventType.Id));

    /// <inheritdoc/>
    public IObservable<AppendedEvent> FilterEventTypes(IObservable<AppendedEvent> observable) => observable.Where(_ => EventTypes.Any(et => et.Id == _.Context.EventType.Id));

    /// <inheritdoc/>
    public void OnNext(ProjectionEventContext context) => _subject.OnNext(context);

    /// <inheritdoc/>
    public bool Accepts(EventType eventType) => _eventTypesToKeyResolver.Keys.Any(_ => _.Id == eventType.Id);

    /// <inheritdoc/>
    public bool HasKeyResolverFor(EventType eventType) => _eventTypesToKeyResolver.ContainsKey(new(eventType.Id, eventType.Generation, eventType.Tombstone));

    /// <inheritdoc/>
    public KeyResolver GetKeyResolverFor(EventType eventType)
    {
        // We only care about the actual event type identifier and generation, any other properties should be the default
        eventType = new(eventType.Id, eventType.Generation, eventType.Tombstone);
        ThrowIfMissingKeyResolverForEventType(eventType);
        return _eventTypesToKeyResolver[eventType];
    }

    /// <inheritdoc/>
    public ProjectionOperationType GetOperationTypeFor(EventType eventType)
    {
        if (OperationTypes.TryGetValue(eventType, out var value))
        {
            return value;
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
        _eventTypesToKeyResolver = eventTypes.ToDictionary(
            _ => new EventType(_.EventType.Id, _.EventType.Generation, _.EventType.Tombstone),
            _ => _.KeyResolver);

        OwnEventTypes = ownEventTypes;
        OperationTypes = operationTypes;
    }

    /// <inheritdoc/>
    public void SetParent(IProjection projection) => Parent = projection;

    /// <inheritdoc/>
    public void Dispose()
    {
        _subject.Dispose();
    }

    void ThrowIfMissingKeyResolverForEventType(EventType eventType)
    {
        if (!_eventTypesToKeyResolver.ContainsKey(eventType))
        {
            throw new MissingKeyResolverForEventType(eventType);
        }
    }
}
