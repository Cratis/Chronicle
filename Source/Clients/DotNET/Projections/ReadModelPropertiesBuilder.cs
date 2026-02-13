// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Properties;
using Cratis.Reflection;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IReadModelPropertiesBuilder{TReadModel, TEvent, TBuilder}"/>.
/// </summary>
/// <param name="projectionBuilder">The parent <see cref="IProjectionBuilderFor{TReadModel}"/>.</param>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for converting names during serialization.</param>
/// <typeparam name="TReadModel">Read model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
/// <typeparam name="TBuilder">Type of actual builder.</typeparam>
/// <typeparam name="TParentBuilder">The type of parent builder.</typeparam>
public class ReadModelPropertiesBuilder<TReadModel, TEvent, TBuilder, TParentBuilder>(IProjectionBuilder<TReadModel, TParentBuilder> projectionBuilder, INamingPolicy namingPolicy)
    : KeyAndParentKeyBuilder<TEvent, TBuilder>(namingPolicy), IReadModelPropertiesBuilder<TReadModel, TEvent, TBuilder>
        where TBuilder : class, IReadModelPropertiesBuilder<TReadModel, TEvent, TBuilder>
        where TParentBuilder : class
{
#pragma warning disable CA1051 // Visible instance fields
#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable SA1629, CA1002, MA0016 // Return abstract
    protected readonly Dictionary<PropertyPath, IPropertyExpressionBuilder> _propertyExpressions = [];
#pragma warning restore CA1629, CA1002, MA0016 // Return abstract
#pragma warning restore CA1600 // Elements should be documented
#pragma warning restore CA1051 // Visible instance fields
    readonly INamingPolicy _namingPolicy = namingPolicy;

    /// <inheritdoc/>
    public TBuilder Increment<TProperty>(Expression<Func<TReadModel, TProperty>> readModelPropertyAccessor)
    {
        var propertyPath = _namingPolicy.GetPropertyName(readModelPropertyAccessor.GetPropertyPath());
        _propertyExpressions[propertyPath] = new IncrementBuilder<TReadModel, TEvent, TProperty>(propertyPath);
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder Decrement<TProperty>(Expression<Func<TReadModel, TProperty>> readModelPropertyAccessor)
    {
        var propertyPath = _namingPolicy.GetPropertyName(readModelPropertyAccessor.GetPropertyPath());
        _propertyExpressions[propertyPath] = new IncrementBuilder<TReadModel, TEvent, TProperty>(propertyPath);
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public IAddBuilder<TReadModel, TEvent, TProperty, TBuilder> Add<TProperty>(Expression<Func<TReadModel, TProperty>> readModelPropertyAccessor)
    {
        var propertyPath = _namingPolicy.GetPropertyName(readModelPropertyAccessor.GetPropertyPath());
        var addBuilder = new AddBuilder<TReadModel, TEvent, TProperty, TBuilder>((this as TBuilder)!, propertyPath, _namingPolicy);
        _propertyExpressions[propertyPath] = addBuilder;
        return addBuilder;
    }

    /// <inheritdoc/>
    public ISubtractBuilder<TReadModel, TEvent, TProperty, TBuilder> Subtract<TProperty>(Expression<Func<TReadModel, TProperty>> readModelPropertyAccessor)
    {
        var propertyPath = _namingPolicy.GetPropertyName(readModelPropertyAccessor.GetPropertyPath());
        var subtractBuilder = new SubtractBuilder<TReadModel, TEvent, TProperty, TBuilder>((this as TBuilder)!, propertyPath, _namingPolicy);
        _propertyExpressions[propertyPath] = subtractBuilder;
        return subtractBuilder;
    }

    /// <inheritdoc/>
    public ISetBuilder<TReadModel, TEvent, TBuilder> Set(PropertyPath propertyPath)
    {
        var propertyInfo = propertyPath.GetPropertyInfoFor<TReadModel>();
        var primitive = propertyInfo.PropertyType.IsAPrimitiveType() || propertyInfo.PropertyType.IsConcept();
        propertyPath = _namingPolicy.GetPropertyName(propertyInfo.Name);
        var setBuilder = new SetBuilder<TReadModel, TEvent, TBuilder>((this as TBuilder)!, propertyPath, _namingPolicy, !primitive);
        _propertyExpressions[propertyPath] = setBuilder;

        return setBuilder;
    }

    /// <inheritdoc/>
    public ISetBuilder<TReadModel, TEvent, TProperty, TBuilder> Set<TProperty>(Expression<Func<TReadModel, TProperty>> readModelPropertyAccessor)
    {
        var targetType = typeof(TProperty);
        var primitive = targetType.IsAPrimitiveType() || targetType.IsConcept();

        var propertyPath = _namingPolicy.GetPropertyName(readModelPropertyAccessor.GetPropertyPath());
        var setBuilder = new SetBuilder<TReadModel, TEvent, TProperty, TBuilder>((this as TBuilder)!, propertyPath, _namingPolicy, !primitive);
        _propertyExpressions[propertyPath] = setBuilder;

        return setBuilder;
    }

    /// <inheritdoc/>
    public ISetBuilder<TReadModel, TEvent, TBuilder> SetThisValue()
    {
        var propertyPath = new PropertyPath(WellKnownExpressions.This);
        var setBuilder = new SetBuilder<TReadModel, TEvent, TBuilder>((this as TBuilder)!, propertyPath, _namingPolicy, false);
        _propertyExpressions[propertyPath] = setBuilder;
        return setBuilder;
    }

    /// <inheritdoc/>
    public TBuilder Count<TProperty>(Expression<Func<TReadModel, TProperty>> readModelPropertyAccessor)
    {
        var propertyPath = _namingPolicy.GetPropertyName(readModelPropertyAccessor.GetPropertyPath());
        _propertyExpressions[propertyPath] = new CountBuilder<TReadModel, TEvent, TProperty>(propertyPath);
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder AddChild<TChildModel>(Expression<Func<TReadModel, IEnumerable<TChildModel>>> targetProperty, Expression<Func<TEvent, TChildModel>> eventProperty)
    {
        var sourcePropertyInfo = eventProperty.GetPropertyInfo();
        if (sourcePropertyInfo.PropertyType.IsAPrimitiveType())
        {
            projectionBuilder
                .Children(targetProperty, childrenBuilder => childrenBuilder
                    .FromEventProperty(eventProperty));
        }
        else
        {
            AddChild(targetProperty, builder => builder.FromObject(eventProperty));
        }

        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder AddChild<TChildModel>(Expression<Func<TReadModel, IEnumerable<TChildModel>>> targetProperty, Action<IAddChildBuilder<TChildModel, TEvent>> builderCallback)
    {
        projectionBuilder.Children(targetProperty, childrenBuilder =>
        {
            childrenBuilder.From<TEvent>(fromBuilder =>
            {
                var builder = new AddChildBuilder<TReadModel, TChildModel, TEvent>(childrenBuilder, fromBuilder, _namingPolicy);
                builderCallback(builder);
            });
        });
        return (this as TBuilder)!;
    }
}
