// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IFromAllBuilder{TReadModel}"/>.
/// </summary>
/// <typeparam name="TReadModel">Type of read model to build for.</typeparam>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for converting names during serialization.</param>
public class FromAllBuilder<TReadModel>(INamingPolicy namingPolicy) : IFromAllBuilder<TReadModel>
{
    readonly List<IPropertyExpressionBuilder> _propertyExpressions = [];

    /// <inheritdoc/>
    public IAllSetBuilder<TReadModel, IFromAllBuilder<TReadModel>> Set<TProperty>(Expression<Func<TReadModel, TProperty>> readModelPropertyAccessor)
    {
        var setBuilder = new AllSetBuilder<TReadModel, IFromAllBuilder<TReadModel>>(this, namingPolicy.GetPropertyName(readModelPropertyAccessor.GetPropertyPath()), namingPolicy);
        _propertyExpressions.Add(setBuilder);
        return setBuilder;
    }

    /// <summary>
    /// Builds a <see cref="FromEveryDefinition"/> from expressions.
    /// </summary>
    /// <returns>A new <see cref="FromEveryDefinition"/> instance.</returns>
    internal FromEveryDefinition Build() => new() { Properties = _propertyExpressions.ToDictionary(_ => (string)_.TargetProperty, _ => _.Build()), IncludeChildren = true };
}
