// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="INestedBuilder{TParentReadModel, TNestedReadModel}"/>.
/// </summary>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for converting names during serialization.</param>
/// <param name="eventTypes"><see cref="IEventTypes"/> for providing event type information.</param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
/// <param name="autoMap">AutoMap behavior for properties - inherits from parent by default.</param>
/// <typeparam name="TParentReadModel">Parent read model type.</typeparam>
/// <typeparam name="TNestedReadModel">Nested read model type.</typeparam>
public class NestedBuilder<TParentReadModel, TNestedReadModel>(
    INamingPolicy namingPolicy,
    IEventTypes eventTypes,
    JsonSerializerOptions jsonSerializerOptions,
    AutoMap autoMap) :
    ProjectionBuilder<TNestedReadModel, INestedBuilder<TParentReadModel, TNestedReadModel>>(namingPolicy, eventTypes, jsonSerializerOptions, autoMap),
    INestedBuilder<TParentReadModel, TNestedReadModel>
{
    /// <inheritdoc/>
    public INestedBuilder<TParentReadModel, TNestedReadModel> ClearWith<TEvent>()
    {
        // Delegates to base class RemovedWith which handles event type validation and registration
        RemovedWith<TEvent>();
        return this;
    }

    /// <summary>
    /// Build the <see cref="ChildrenDefinition"/> representing the nested object.
    /// </summary>
    /// <returns>A new <see cref="ChildrenDefinition"/>.</returns>
    internal ChildrenDefinition Build() =>
        new()
        {
            IdentifiedBy = PropertyPath.NotSet,
            From = _fromDefinitions,
            Join = _joinDefinitions,
            Children = _childrenDefinitions.ToDictionary(_ => (string)_.Key, _ => _.Value),
            All = _fromEveryDefinition,
            RemovedWith = _removedWithDefinitions,
            RemovedWithJoin = _removedWithJoinDefinitions,
            Nested = _nestedDefinitions.ToDictionary(_ => (string)_.Key, _ => _.Value)
        };
}
