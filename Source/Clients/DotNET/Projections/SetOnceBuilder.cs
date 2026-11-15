// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents a <see cref="SetBuilder{TReadModel, TEvent, TProperty, TParentBuilder}"/> that only assigns the value when
/// the target property has no value yet.
/// </summary>
/// <param name="parent">Parent builder.</param>
/// <param name="targetProperty">Target property.</param>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for converting names during serialization.</param>
/// <param name="forceEventProperty">Whether or not to force event property - only event property is supported.</param>
/// <typeparam name="TReadModel">Read model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
/// <typeparam name="TProperty">The type of the property.</typeparam>
/// <typeparam name="TParentBuilder">Type of the parent builder.</typeparam>
/// <remarks>
/// The value expression is wrapped in the set once expression. The first event that produces a value wins; later events leave
/// the property as it is - on replay too, since the outcome only depends on the order of the events.
/// </remarks>
public class SetOnceBuilder<TReadModel, TEvent, TProperty, TParentBuilder>(TParentBuilder parent, PropertyPath targetProperty, INamingPolicy namingPolicy, bool forceEventProperty = false)
    : SetBuilder<TReadModel, TEvent, TProperty, TParentBuilder>(parent, targetProperty, namingPolicy, forceEventProperty)
{
    /// <inheritdoc/>
    public override string Build() => $"{WellKnownExpressions.SetOnce}({base.Build()})";
}
