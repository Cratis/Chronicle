// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Properties;
using Aksio.Cratis.Shared.Events;
using NJsonSchema;

namespace Aksio.Cratis.Kernel.Engines.Projections.Expressions;

/// <summary>
/// Defines a resolver of expressions related to target properties on the model.
/// </summary>
public interface IModelPropertyExpressionResolver
{
    /// <summary>
    /// Called to check if the resolver can resolve the expression.
    /// </summary>
    /// <param name="targetProperty">The target property we're mapping to.</param>
    /// <param name="expression">Expression to check.</param>
    /// <returns>True if it can resolve, false if not.</returns>
    bool CanResolve(PropertyPath targetProperty, string expression);

    /// <summary>
    /// Called to resolve the expression.
    /// </summary>
    /// <param name="targetProperty">The target property we're mapping to.</param>
    /// <param name="targetPropertySchema">The target properties <see cref="JsonSchemaProperty"/>.</param>
    /// <param name="expression">Expression to resolve.</param>
    /// <returns><see cref="PropertyMapper{Event, ExpandoObject}"/> it resolves to.</returns>
    PropertyMapper<AppendedEvent, ExpandoObject> Resolve(PropertyPath targetProperty, JsonSchemaProperty targetPropertySchema, string expression);
}
