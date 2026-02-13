// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Projections.Expressions.EventValues;
using Cratis.Chronicle.Projections.Expressions.ReadModelProperties;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.Expressions;

/// <summary>
/// Represents an implementation of <see cref="IReadModelPropertyExpressionResolvers"/>.
/// </summary>
/// <param name="eventValueProviderExpressionResolvers"><see cref="IEventValueProviderExpressionResolvers"/> to use for value provider resolvers.</param>
/// <param name="typeFormats"><see cref="ITypeFormats"/> to use for correct type conversion.</param>
/// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
public partial class ReadModelPropertyExpressionResolvers(
    IEventValueProviderExpressionResolvers eventValueProviderExpressionResolvers,
    ITypeFormats typeFormats,
    ILogger<ReadModelPropertyExpressionResolvers> logger) : IReadModelPropertyExpressionResolvers
{
    readonly IReadModelPropertyExpressionResolver[] _resolvers =
        [
            new AddExpressionResolver(eventValueProviderExpressionResolvers, typeFormats),
            new SubtractExpressionResolver(eventValueProviderExpressionResolvers, typeFormats),
            new CountExpressionResolver(typeFormats),
            new IncrementExpressionResolver(typeFormats),
            new DecrementExpressionResolver(typeFormats),
            new SetExpressionResolver(eventValueProviderExpressionResolvers)
        ];

    /// <inheritdoc/>
    public bool CanResolve(PropertyPath targetProperty, string expression) =>
        _resolvers.Any(_ => _.CanResolve(targetProperty, expression));

    /// <inheritdoc/>
    public PropertyMapper<AppendedEvent, ExpandoObject> Resolve(PropertyPath targetProperty, JsonSchemaProperty targetPropertySchema, string expression)
    {
        var resolver = Array.Find(_resolvers, _ => _.CanResolve(targetProperty, expression));
        ThrowIfUnsupportedModelPropertyExpression(targetProperty, expression, resolver);
        return resolver!.Resolve(targetProperty, targetPropertySchema, expression);
    }

    void ThrowIfUnsupportedModelPropertyExpression(PropertyPath targetProperty, string expression, IReadModelPropertyExpressionResolver? resolver)
    {
        if (resolver == default)
        {
            logger.UnsupportedReadModelPropertyExpression(expression, targetProperty);
            throw new UnsupportedReadModelPropertyExpression(expression);
        }
    }
}
