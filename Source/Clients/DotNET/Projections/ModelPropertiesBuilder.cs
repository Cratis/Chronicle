// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Properties;
using Cratis.Reflection;
using Cratis.Strings;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IModelPropertiesBuilder{TModel, TEvent, TBuilder}"/>.
/// </summary>
/// <param name="projectionBuilder">The parent <see cref="IProjectionBuilderFor{TModel}"/>.</param>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
/// <typeparam name="TBuilder">Type of actual builder.</typeparam>
/// <typeparam name="TParentBuilder">The type of parent builder.</typeparam>
public class ModelPropertiesBuilder<TModel, TEvent, TBuilder, TParentBuilder>(IProjectionBuilder<TModel, TParentBuilder> projectionBuilder)
    : KeyAndParentKeyBuilder<TEvent, TBuilder>, IModelPropertiesBuilder<TModel, TEvent, TBuilder>
        where TBuilder : class, IModelPropertiesBuilder<TModel, TEvent, TBuilder>
        where TParentBuilder : class
{
#pragma warning disable CA1051 // Visible instance fields
#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable SA1629, CA1002, MA0016 // Return abstract
    protected readonly Dictionary<PropertyPath, IPropertyExpressionBuilder> _propertyExpressions = [];
#pragma warning restore CA1629, CA1002, MA0016 // Return abstract
#pragma warning restore CA1600 // Elements should be documented
#pragma warning restore CA1051 // Visible instance fields

    /// <inheritdoc/>
    public TBuilder AutoMap()
    {
        var eventProperties = typeof(TEvent).GetProperties().Select(_ => _.Name.ToCamelCase());
        var modelProperties = typeof(TModel).GetProperties().Select(_ => _.Name.ToCamelCase());

        foreach (var property in eventProperties.Intersect(modelProperties))
        {
            Set(property).To(property);
        }

        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder Increment<TProperty>(Expression<Func<TModel, TProperty>> modelPropertyAccessor)
    {
        var propertyPath = modelPropertyAccessor.GetPropertyPath();
        _propertyExpressions[propertyPath] = new IncrementBuilder<TModel, TEvent, TProperty>(propertyPath);
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder Decrement<TProperty>(Expression<Func<TModel, TProperty>> modelPropertyAccessor)
    {
        var propertyPath = modelPropertyAccessor.GetPropertyPath();
        _propertyExpressions[propertyPath] = new IncrementBuilder<TModel, TEvent, TProperty>(propertyPath);
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public IAddBuilder<TModel, TEvent, TProperty, TBuilder> Add<TProperty>(Expression<Func<TModel, TProperty>> modelPropertyAccessor)
    {
        var propertyPath = modelPropertyAccessor.GetPropertyPath();
        var addBuilder = new AddBuilder<TModel, TEvent, TProperty, TBuilder>((this as TBuilder)!, propertyPath);
        _propertyExpressions[propertyPath] = addBuilder;
        return addBuilder;
    }

    /// <inheritdoc/>
    public ISubtractBuilder<TModel, TEvent, TProperty, TBuilder> Subtract<TProperty>(Expression<Func<TModel, TProperty>> modelPropertyAccessor)
    {
        var propertyPath = modelPropertyAccessor.GetPropertyPath();
        var subtractBuilder = new SubtractBuilder<TModel, TEvent, TProperty, TBuilder>((this as TBuilder)!, propertyPath);
        _propertyExpressions[propertyPath] = subtractBuilder;
        return subtractBuilder;
    }

    /// <inheritdoc/>
    public ISetBuilder<TModel, TEvent, TBuilder> Set(PropertyPath propertyPath)
    {
        var propertyInfo = propertyPath.GetPropertyInfoFor<TModel>();
        var primitive = propertyInfo.PropertyType.IsAPrimitiveType() || propertyInfo.PropertyType.IsConcept();
        var setBuilder = new SetBuilder<TModel, TEvent, TBuilder>((this as TBuilder)!, propertyPath, !primitive);
        _propertyExpressions[propertyPath] = setBuilder;

        return setBuilder;
    }

    /// <inheritdoc/>
    public ISetBuilder<TModel, TEvent, TProperty, TBuilder> Set<TProperty>(Expression<Func<TModel, TProperty>> modelPropertyAccessor)
    {
        var targetType = typeof(TProperty);
        var primitive = targetType.IsAPrimitiveType() || targetType.IsConcept();

        var propertyPath = modelPropertyAccessor.GetPropertyPath();
        var setBuilder = new SetBuilder<TModel, TEvent, TProperty, TBuilder>((this as TBuilder)!, propertyPath, !primitive);
        _propertyExpressions[propertyPath] = setBuilder;

        return setBuilder;
    }

    /// <inheritdoc/>
    public ISetBuilder<TModel, TEvent, TBuilder> SetThisValue()
    {
        var propertyPath = new PropertyPath("$this");
        var setBuilder = new SetBuilder<TModel, TEvent, TBuilder>((this as TBuilder)!, propertyPath, false);
        _propertyExpressions[propertyPath] = setBuilder;
        return setBuilder;
    }

    /// <inheritdoc/>
    public TBuilder Count<TProperty>(Expression<Func<TModel, TProperty>> modelPropertyAccessor)
    {
        var propertyPath = modelPropertyAccessor.GetPropertyPath();
        _propertyExpressions[propertyPath] = new CountBuilder<TModel, TEvent, TProperty>(propertyPath);
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder AddChild<TChildModel>(Expression<Func<TModel, IEnumerable<TChildModel>>> targetProperty, Expression<Func<TEvent, TChildModel>> eventProperty)
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
    public TBuilder AddChild<TChildModel>(Expression<Func<TModel, IEnumerable<TChildModel>>> targetProperty, Action<IAddChildBuilder<TChildModel, TEvent>> builderCallback)
    {
        projectionBuilder.Children(targetProperty, childrenBuilder =>
        {
            childrenBuilder.From<TEvent>(fromBuilder =>
            {
                var builder = new AddChildBuilder<TModel, TChildModel, TEvent>(childrenBuilder, fromBuilder);
                builderCallback(builder);
            });
        });
        return (this as TBuilder)!;
    }
}
