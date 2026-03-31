// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionBuilderFor{TReadModel}"/>.
/// </summary>
/// <typeparam name="TReadModel">Type of read model.</typeparam>
public class ProjectionBuilderFor<TReadModel> : ProjectionBuilder<TReadModel, IProjectionBuilderFor<TReadModel>>, IProjectionBuilderFor<TReadModel>
{
    readonly ProjectionId _identifier;
    readonly Type _projectionType;
    EventSequenceId _eventSequenceId = EventSequenceId.Log;
    bool _eventSequenceExplicitlySet;
    bool _isRewindable = true;
    bool _isActive = true;

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
        _readModelIdentifier = typeof(TReadModel).GetReadModelIdentifier();
    }

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

    /// <summary>
    /// Build the projection definition.
    /// </summary>
    /// <returns><see cref="ProjectionDefinition"/>.</returns>
    /// <exception cref="MultipleEventStoresDefined">Thrown when event types from multiple event stores are used without an explicit event sequence.</exception>
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
            RemovedWith = _removedWithDefinitions,
            Tags = _projectionType.GetTags().ToArray(),
            AutoMap = (Contracts.Projections.AutoMap)_autoMap
        };
    }
}
