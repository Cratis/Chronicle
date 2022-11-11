// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Aksio.Cratis.Properties;

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

    /// <summary>
    /// Initializes a new instance of the <see cref="AddChildBuilder{TParentModel, TChildModel, TEvent, TParentBuilder}"/> class.
    /// </summary>
    /// <param name="childrenBuilder">The children builder to use internally.</param>
    /// <param name="parentBuilder">THe parent builder to continue build on for the fluent interface.</param>
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
        _childrenBuilder.From<TEvent>(_ =>
        {
            foreach (var property in typeof(TChildModel).GetProperties())
            {
                var propertyPath = new PropertyPath(property.Name);
                _.Set(propertyPath).To(propertyPath);
            }
        });

        return _parentBuilder;
    }
}
