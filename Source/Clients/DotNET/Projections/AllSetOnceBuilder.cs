// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an <see cref="AllSetBuilder{TReadModel, TParentBuilder}"/> that only assigns the value when the target
/// property has no value yet.
/// </summary>
/// <param name="parent">Parent builder.</param>
/// <param name="targetProperty">Target property.</param>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for converting names during serialization.</param>
/// <typeparam name="TReadModel">Read model to build for.</typeparam>
/// <typeparam name="TParentBuilder">Type of the parent builder.</typeparam>
public class AllSetOnceBuilder<TReadModel, TParentBuilder>(TParentBuilder parent, PropertyPath targetProperty, INamingPolicy namingPolicy)
    : AllSetBuilder<TReadModel, TParentBuilder>(parent, targetProperty, namingPolicy)
{
    /// <inheritdoc/>
    public override string Build() => $"{WellKnownExpressions.SetOnce}({base.Build()})";
}
