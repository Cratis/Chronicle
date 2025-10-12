// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Text;
using Cratis.Chronicle.Properties;
using Cratis.Reflection;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="ICompositeKeyBuilder{TKeyType, TEvent}"/>.
/// </summary>
/// <typeparam name="TKeyType">Type of key to build.</typeparam>
/// <typeparam name="TEvent">Event to build from.</typeparam>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for property names.</param>
public class CompositeKeyBuilder<TKeyType, TEvent>(INamingPolicy namingPolicy) : ICompositeKeyBuilder<TKeyType, TEvent>
{
    readonly List<IPropertyExpressionBuilder> _propertyExpressions = [];

    /// <inheritdoc/>
    public ISetBuilder<TKeyType, TEvent, TProperty, ICompositeKeyBuilder<TKeyType, TEvent>> Set<TProperty>(Expression<Func<TKeyType, TProperty>> readModelPropertyAccessor)
    {
        var targetType = typeof(TProperty);
        var primitive = targetType.IsAPrimitiveType() || targetType.IsConcept();

        var setBuilder = new SetBuilder<TKeyType, TEvent, TProperty, ICompositeKeyBuilder<TKeyType, TEvent>>(
            this,
            namingPolicy.GetPropertyName(readModelPropertyAccessor.GetPropertyPath()),
            namingPolicy,
            !primitive);
        _propertyExpressions.Add(setBuilder);
        return setBuilder;
    }

    /// <inheritdoc/>
    public PropertyExpression Build()
    {
        var expressionBuilder = new StringBuilder();
        expressionBuilder
            .Append("$composite(")
            .AppendJoin(',', _propertyExpressions.Select(_ => $"{_.TargetProperty}={_.Build()}"))
            .Append(')');
        return expressionBuilder.ToString();
    }
}
