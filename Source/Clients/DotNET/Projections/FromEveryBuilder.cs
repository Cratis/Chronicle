// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IFromEveryBuilder{TReadModel}"/>.
/// </summary>
/// <typeparam name="TReadModel">Type of read model to build for.</typeparam>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for converting names during serialization.</param>
public class FromEveryBuilder<TReadModel>(INamingPolicy namingPolicy) : IFromEveryBuilder<TReadModel>
{
    readonly List<IPropertyExpressionBuilder> _propertyExpressions = [];
    bool _includeChildren = true;

    /// <inheritdoc/>
    public IAllSetBuilder<TReadModel, IFromEveryBuilder<TReadModel>> Set<TProperty>(Expression<Func<TReadModel, TProperty>> readModelPropertyAccessor)
    {
        var setBuilder = new AllSetBuilder<TReadModel, IFromEveryBuilder<TReadModel>>(this, namingPolicy.GetPropertyName(readModelPropertyAccessor.GetPropertyPath()), namingPolicy);
        _propertyExpressions.Add(setBuilder);
        return setBuilder;
    }

    /// <inheritdoc/>
    public IFromEveryBuilder<TReadModel> ExcludeChildProjections()
    {
        _includeChildren = false;
        return this;
    }

    /// <summary>
    /// Builds a <see cref="FromEveryDefinition"/> from expressions.
    /// </summary>
    /// <returns>A new <see cref="FromEveryDefinition"/> instance.</returns>
    internal FromEveryDefinition Build() => new() { Properties = _propertyExpressions.ToDictionary(_ => (string)_.TargetProperty, _ => _.Build()), IncludeChildren = _includeChildren };
}
