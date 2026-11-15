// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.Expressions.ReadModelProperties;

/// <summary>
/// Represents a <see cref="IReadModelPropertyExpressionResolver"/> for removing a dictionary key addressed by a dynamic key segment.
/// </summary>
public class RemoveExpressionResolver : IReadModelPropertyExpressionResolver
{
    /// <inheritdoc/>
    public bool CanResolve(PropertyPath targetProperty, string expression) => expression == WellKnownExpressions.Remove;

    /// <inheritdoc/>
    public PropertyMapper<AppendedEvent, ExpandoObject> Resolve(PropertyPath targetProperty, JsonSchemaProperty targetPropertySchema, string expression)
    {
        if (!targetProperty.Path.Contains(".$", StringComparison.Ordinal))
        {
            throw new RemoveRequiresDynamicPropertyPath(targetProperty);
        }

        return PropertyMappers.Remove(targetProperty);
    }
}
