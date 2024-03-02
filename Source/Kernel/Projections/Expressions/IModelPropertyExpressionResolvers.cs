// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Events;
using Aksio.Cratis.Properties;
using NJsonSchema;

namespace Aksio.Cratis.Kernel.Projections.Expressions;

/// <summary>
/// Defines a system for resolving a model property expression. It represents known expression resolvers in the system.
/// </summary>
public interface IModelPropertyExpressionResolvers
{
    /// <summary>
    /// Called to verify if the resolver can resolve the expression.
    /// </summary>
    /// <param name="targetProperty">The target property we're mapping to.</param>
    /// <param name="expression">Expression to resolve.</param>
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
