// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Kernel.Contracts.Projections;
using Cratis.Properties;

namespace Cratis.Projections;

/// <summary>
/// Represents an implementation of <see cref="IFromEveryBuilder{TModel}"/>.
/// </summary>
/// <typeparam name="TModel">Type of model to build for.</typeparam>
public class FromEveryBuilder<TModel> : IFromEveryBuilder<TModel>
{
    readonly List<IPropertyExpressionBuilder> _propertyExpressions = [];
    bool _includeChildren;

    /// <inheritdoc/>
    public IAllSetBuilder<TModel, IFromEveryBuilder<TModel>> Set<TProperty>(Expression<Func<TModel, TProperty>> modelPropertyAccessor)
    {
        var setBuilder = new AllSetBuilder<TModel, IFromEveryBuilder<TModel>>(this, modelPropertyAccessor.GetPropertyPath());
        _propertyExpressions.Add(setBuilder);
        return setBuilder;
    }

    /// <inheritdoc/>
    public IFromEveryBuilder<TModel> IncludeChildProjections()
    {
        _includeChildren = true;
        return this;
    }

    /// <summary>
    /// Builds a <see cref="AllDefinition"/> from expressions.
    /// </summary>
    /// <returns>A new <see cref="AllDefinition"/> instance.</returns>
    public AllDefinition Build() => new() { Properties = _propertyExpressions.ToDictionary(_ => (string)_.TargetProperty, _ => _.Build()), IncludeChildren = _includeChildren };
}
