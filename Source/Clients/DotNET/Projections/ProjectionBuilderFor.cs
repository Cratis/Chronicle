// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Serialization;
using EventType = Cratis.Chronicle.Contracts.Events.EventType;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionBuilderFor{TReadModel}"/>.
/// </summary>
/// <typeparam name="TReadModel">Type of read model.</typeparam>
public class ProjectionBuilderFor<TReadModel> : ProjectionBuilder<TReadModel, IProjectionBuilderFor<TReadModel>>, IProjectionBuilderFor<TReadModel>
{
    readonly ProjectionId _identifier;
    readonly Type _projectionType;
    readonly INamingPolicy _namingPolicy;
    readonly IEventTypes _eventTypes;
    readonly List<EventType> _enteringEventTypes = [];
    EventSequenceId _eventSequenceId = EventSequenceId.Log;
    bool _eventSequenceExplicitlySet;
    bool _isRewindable = true;
    bool _isActive = true;
    Type? _variantIdentity;
    PropertyPath _variantKey = PropertyPath.NotSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionBuilderFor{TReadModel}"/> class.
    /// </summary>
    /// <param name="identifier">The unique identifier for the projection.</param>
    /// <param name="projectionType">The type of the projection.</param>
    /// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for converting names during serialization.</param>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for providing event type information.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
    public ProjectionBuilderFor(
        ProjectionId identifier,
        Type projectionType,
        INamingPolicy namingPolicy,
        IEventTypes eventTypes,
        JsonSerializerOptions jsonSerializerOptions)
        : base(namingPolicy, eventTypes, jsonSerializerOptions, Chronicle.Projections.AutoMap.Enabled)
    {
        _identifier = identifier;
        _projectionType = projectionType;
        _namingPolicy = namingPolicy;
        _eventTypes = eventTypes;
        _readModelIdentifier = typeof(TReadModel).GetReadModelIdentifier();
    }

    /// <summary>
    /// Gets what this projection declared about being one variant of a mutually exclusive group, or default when
    /// it is an ordinary single-read-model projection.
    /// </summary>
    internal FluentVariantDeclaration? VariantDeclaration =>
        _variantIdentity is null ? null : new FluentVariantDeclaration(_variantIdentity, _enteringEventTypes);

    /// <inheritdoc/>
    public IProjectionBuilderFor<TReadModel> FromEventSequence(EventSequenceId eventSequenceId)
    {
        _eventSequenceId = eventSequenceId;
        _eventSequenceExplicitlySet = true;
        return this;
    }

    /// <inheritdoc/>
    public IProjectionBuilderFor<TReadModel> ContainerName(string containerName)
    {
        _readModelIdentifier = containerName;
        return this;
    }

    /// <inheritdoc/>
    public IProjectionBuilderFor<TReadModel> NotRewindable()
    {
        _isRewindable = false;
        return this;
    }

    /// <inheritdoc/>
    public IProjectionBuilderFor<TReadModel> Passive()
    {
        _isActive = false;
        return this;
    }

    /// <inheritdoc/>
    public IProjectionBuilderFor<TReadModel> VariantOf<TIdentity>(Expression<Func<TReadModel, object?>> keyAccessor)
    {
        if (!keyAccessor.TryGetPropertyPath(out var propertyPath))
        {
            throw new InvalidPropertyExpression($"the variant key property on read model '{typeof(TReadModel).FullName}'", keyAccessor);
        }

        _variantIdentity = typeof(TIdentity);
        _variantKey = _namingPolicy.GetPropertyName(propertyPath);
        return this;
    }

    /// <inheritdoc/>
    public IProjectionBuilderFor<TReadModel> EntersOn<TEvent>()
    {
        var type = typeof(TEvent);

        if (!type.IsEventType(_eventTypes.AllClrTypes))
        {
            throw new TypeIsNotAnEventType(type);
        }

        var eventType = _eventTypes.GetEventTypeFor(type).ToContract();
        _enteringEventTypes.Add(eventType);

        // The entering event is what creates the variant, so it must have a From even when the author maps no
        // properties on it explicitly and leaves the mapping to AutoMap.
        if (!_fromDefinitions.ContainsKey(eventType))
        {
            _fromDefinitions[eventType] = new FromDefinition
            {
                Key = WellKnownExpressions.EventSourceId,
                Properties = new Dictionary<string, string>()
            };
        }

        CollectEventStore(type);

        return this;
    }

    /// <summary>
    /// Build the projection definition.
    /// </summary>
    /// <returns><see cref="ProjectionDefinition"/>.</returns>
    /// <exception cref="MultipleEventStoresDefined">Thrown when event types from multiple event stores are used without an explicit event sequence.</exception>
    /// <exception cref="VariantMustDeclareEntersOnEvent">Thrown when the projection is declared as a variant without naming the event that activates it.</exception>
    internal ProjectionDefinition Build()
    {
        var eventSequenceId = _eventSequenceId;

        if (!_eventSequenceExplicitlySet && _observedEventStores.Count > 0)
        {
            var distinctStores = _observedEventStores.Distinct().ToList();
            if (distinctStores.Count > 1)
            {
                throw new MultipleEventStoresDefined(_projectionType, distinctStores);
            }

            eventSequenceId = new EventSequenceId($"{EventSequenceId.InboxPrefix}{distinctStores[0]}");
        }

        if (_variantIdentity is not null)
        {
            if (_enteringEventTypes.Count == 0)
            {
                throw new VariantMustDeclareEntersOnEvent(_projectionType);
            }

            VariantReclassifier.Reclassify(_fromDefinitions, _joinDefinitions, _enteringEventTypes, _variantKey);
        }

        return new()
        {
            EventSequenceId = eventSequenceId,
            Identifier = _identifier,
            ReadModel = _readModelIdentifier,
            IsActive = _isActive,
            IsRewindable = _isRewindable,
            InitialModelState = _initialValues.ToJsonString(),
            From = _fromDefinitions,
            Join = _joinDefinitions,
            Children = _childrenDefinitions.ToDictionary(_ => (string)_.Key, _ => _.Value),
            All = _fromEveryDefinition,
            SubscribesToAllEvents = SubscribesToAllEvents,
            RemovedWith = _removedWithDefinitions,
            Tags = _projectionType.GetTags().ToArray(),
            AutoMap = (Contracts.Projections.AutoMap)_autoMap,
            NoAutoMapProperties = [.. _noAutoMapProperties],
            Nested = _nestedDefinitions.ToDictionary(_ => (string)_.Key, _ => _.Value)
        };
    }
}
