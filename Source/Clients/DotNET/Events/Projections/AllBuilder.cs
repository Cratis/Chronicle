// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Reflection;

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Represents an implementation of <see cref="IAllBuilder{TModel}"/>.
/// </summary>
/// <typeparam name="TModel">Type of model to build for.</typeparam>
public class AllBuilder<TModel> : IAllBuilder<TModel>
{
    readonly List<IPropertyExpressionBuilder> _propertyExpressions = new();
    bool _includeChildren;

    /// <inheritdoc/>
    public IAllSetBuilder<TModel, IAllBuilder<TModel>> Set<TProperty>(Expression<Func<TModel, TProperty>> modelPropertyAccessor)
    {
        var setBuilder = new AllSetBuilder<TModel, IAllBuilder<TModel>>(this, modelPropertyAccessor.GetPropertyPath());
        _propertyExpressions.Add(setBuilder);
        return setBuilder;
    }

    /// <inheritdoc/>
    public IAllBuilder<TModel> IncludeChildProjections()
    {
        _includeChildren = true;
        return this;
    }

    /// <summary>
    /// Builds a <see cref="AllDefinition"/> from expressions.
    /// </summary>
    /// <returns>A new <see cref="AllDefinition"/> instance.</returns>
    public AllDefinition Build() => new(_propertyExpressions.ToDictionary(_ => _.TargetProperty, _ => _.Build()), _includeChildren);
}
