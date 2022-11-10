// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Represents an implementation of <see cref="IAddChildBuilder{TChildModel, TEvent, TParentBuilder}"/>.
/// </summary>
/// <typeparam name="TParentModel">Parent model type.</typeparam>
/// <typeparam name="TChildModel">Child model type.</typeparam>
/// <typeparam name="TEvent">Type of the event.</typeparam>
/// <typeparam name="TParentBuilder">Type of the parent builder.</typeparam>
public class AddChildBuilder<TParentModel, TChildModel, TEvent, TParentBuilder> : IAddChildBuilder<TChildModel, TEvent, TParentBuilder>
{
    readonly IChildrenBuilder<TParentModel, TChildModel> _childrenBuilder;
    readonly TParentBuilder _parentBuilder;

    public AddChildBuilder(IChildrenBuilder<TParentModel, TChildModel> childrenBuilder, TParentBuilder parentBuilder)
    {
        _childrenBuilder = childrenBuilder;
        _parentBuilder = parentBuilder;
    }

    /// <inheritdoc/>
    public TParentBuilder IdentifiedBy<TProperty>(Expression<Func<TChildModel, TProperty>> propertyExpression)
    {
        _childrenBuilder.IdentifiedBy(propertyExpression);
        return _parentBuilder;
    }

    /// <inheritdoc/>
    public TParentBuilder FromObject(Expression<Func<TEvent, IEnumerable<TChildModel>>> propertyWithChild)
    {
        return _parentBuilder;
    }
}
