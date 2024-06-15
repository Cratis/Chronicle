// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Properties;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.Expressions.EventValues;

/// <summary>
/// Defines a system for resolving a event value provider expression. It represents known expression resolvers in the system.
/// </summary>
public interface IEventValueProviderExpressionResolvers
{
    /// <summary>
    /// Called to verify if the resolver can resolve the expression.
    /// </summary>
    /// <param name="expression">Expression to resolve.</param>
    /// <returns>True if it can resolve, false if not.</returns>
    bool CanResolve(string expression);

    /// <summary>
    /// Called to resolve the expression.
    /// </summary>
    /// <param name="targetSchemaProperty"><see cref="JsonSchemaProperty"/> representing the target property we're resolving for.</param>
    /// <param name="expression">Expression to resolve.</param>
    /// <returns><see cref="ValueProvider{AppendedEvent}"/> it resolves to.</returns>
    /// <remarks>
    /// By having the target property, we know what type is expected and can resolve the value to the expected target type.
    /// </remarks>
    ValueProvider<AppendedEvent> Resolve(JsonSchemaProperty targetSchemaProperty, string expression);
}
