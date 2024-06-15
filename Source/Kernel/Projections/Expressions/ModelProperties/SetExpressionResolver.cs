// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Projections.Expressions.EventValues;
using Cratis.Events;
using Cratis.Properties;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.Expressions.ModelProperties;

/// <summary>
/// Represents a <see cref="IModelPropertyExpressionResolver"/> for setting a property on a model with the value for a property based event value expressions.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AddExpressionResolver"/> class.
/// </remarks>
/// <param name="eventValueProviderExpressionResolvers"><see cref="IEventValueProviderExpressionResolvers"/> for resolving.</param>
public class SetExpressionResolver(IEventValueProviderExpressionResolvers eventValueProviderExpressionResolvers) : IModelPropertyExpressionResolver
{
    /// <inheritdoc/>
    public bool CanResolve(PropertyPath targetProperty, string expression) => eventValueProviderExpressionResolvers.CanResolve(expression);

    /// <inheritdoc/>
    public PropertyMapper<AppendedEvent, ExpandoObject> Resolve(PropertyPath targetProperty, JsonSchemaProperty targetPropertySchema, string expression) =>
        PropertyMappers.FromEventValueProvider(targetProperty, eventValueProviderExpressionResolvers.Resolve(targetPropertySchema, expression));
}
