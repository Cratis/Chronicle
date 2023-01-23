// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Aksio.Cratis.Events;
using Aksio.Cratis.Properties;
using Aksio.Cratis.Reflection;

namespace Aksio.Cratis.Projections;

/// <summary>
/// Represents an implementation of <see cref="IAddChildBuilder{TChildModel, TEvent}"/>.
/// </summary>
/// <typeparam name="TParentModel">Parent model type.</typeparam>
/// <typeparam name="TChildModel">Child model type.</typeparam>
/// <typeparam name="TEvent">Type of the event.</typeparam>
public class AddChildBuilder<TParentModel, TChildModel, TEvent> : IAddChildBuilder<TChildModel, TEvent>
{
    readonly IChildrenBuilder<TParentModel, TChildModel> _childrenBuilder;
    readonly IFromBuilder<TChildModel, TEvent> _fromBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddChildBuilder{TParentModel, TChildModel, TEvent}"/> class.
    /// </summary>
    /// <param name="childrenBuilder">The children builder to use internally.</param>
    /// <param name="fromBuilder">The <see cref="IFromBuilder{TModel, TEvent}"/> to build the internals of the child relationship.</param>
    public AddChildBuilder(IChildrenBuilder<TParentModel, TChildModel> childrenBuilder, IFromBuilder<TChildModel, TEvent> fromBuilder)
    {
        _childrenBuilder = childrenBuilder;
        _fromBuilder = fromBuilder;
    }

    /// <inheritdoc/>
    public IAddChildBuilder<TChildModel, TEvent> UsingKey<TProperty>(Expression<Func<TEvent, TProperty>> keyAccessor)
    {
        _fromBuilder!.UsingKey(keyAccessor);
        return this;
    }

    /// <inheritdoc/>
    public IAddChildBuilder<TChildModel, TEvent> UsingKeyFromContext(Expression<Func<TEvent, EventContext>> keyAccessor)
    {
        _fromBuilder!.UsingKeyFromContext(keyAccessor);
        return this;
    }

    /// <inheritdoc/>
    public IAddChildBuilder<TChildModel, TEvent> UsingParentKey<TProperty>(Expression<Func<TEvent, TProperty>> keyAccessor)
    {
        _fromBuilder!.UsingParentKey(keyAccessor);
        return this;
    }

    /// <inheritdoc/>
    public IAddChildBuilder<TChildModel, TEvent> UsingParentKeyFromContext<TProperty>(Expression<Func<TEvent, TProperty>> keyAccessor)
    {
        _fromBuilder!.UsingParentKeyFromContext(keyAccessor);
        return this;
    }

    /// <inheritdoc/>
    public IAddChildBuilder<TChildModel, TEvent> IdentifiedBy<TProperty>(Expression<Func<TChildModel, TProperty>> propertyExpression)
    {
        _childrenBuilder.IdentifiedBy(propertyExpression);
        return this;
    }

    /// <inheritdoc/>
    public IAddChildBuilder<TChildModel, TEvent> FromObject(Expression<Func<TEvent, TChildModel>> propertyWithChild)
    {
        var childProperty = propertyWithChild.GetPropertyPath();
        foreach (var property in typeof(TChildModel).GetProperties())
        {
            var propertyPath = new PropertyPath(property.Name);
            _fromBuilder!.Set(propertyPath).To(childProperty + propertyPath);
        }

        return this;
    }
}
