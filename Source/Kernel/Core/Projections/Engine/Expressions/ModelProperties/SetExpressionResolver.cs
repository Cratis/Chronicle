// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Properties;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.Engine.Expressions.ReadModelProperties;

/// <summary>
/// Represents a <see cref="IReadModelPropertyExpressionResolver"/> for setting a property on a model with the value for a property based event value expressions.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AddExpressionResolver"/> class.
/// </remarks>
/// <param name="eventValueProviderExpressionResolvers"><see cref="IEventValueProviderExpressionResolvers"/> for resolving.</param>
public class SetExpressionResolver(IEventValueProviderExpressionResolvers eventValueProviderExpressionResolvers) : IReadModelPropertyExpressionResolver
{
    /// <inheritdoc/>
    public bool CanResolve(PropertyPath targetProperty, string expression) => eventValueProviderExpressionResolvers.CanResolve(expression);

    /// <inheritdoc/>
    public PropertyMapper<AppendedEvent, ExpandoObject> Resolve(PropertyPath targetProperty, JsonSchemaProperty targetPropertySchema, string expression) =>
        PropertyMappers.FromEventValueProvider(targetProperty, eventValueProviderExpressionResolvers.Resolve(targetPropertySchema, expression));
}
