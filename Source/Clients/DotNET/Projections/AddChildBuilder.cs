// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Events;
using Cratis.Properties;
using Cratis.Reflection;

namespace Cratis.Projections;

/// <summary>
/// Represents an implementation of <see cref="IAddChildBuilder{TChildModel, TEvent}"/>.
/// </summary>
/// <typeparam name="TParentModel">Parent model type.</typeparam>
/// <typeparam name="TChildModel">Child model type.</typeparam>
/// <typeparam name="TEvent">Type of the event.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="AddChildBuilder{TParentModel, TChildModel, TEvent}"/> class.
/// </remarks>
/// <param name="childrenBuilder">The children builder to use internally.</param>
/// <param name="fromBuilder">The <see cref="IFromBuilder{TModel, TEvent}"/> to build the internals of the child relationship.</param>
public class AddChildBuilder<TParentModel, TChildModel, TEvent>(IChildrenBuilder<TParentModel, TChildModel> childrenBuilder, IFromBuilder<TChildModel, TEvent> fromBuilder) : IAddChildBuilder<TChildModel, TEvent>
{
    /// <inheritdoc/>
    public IAddChildBuilder<TChildModel, TEvent> UsingKey<TProperty>(Expression<Func<TEvent, TProperty>> keyAccessor)
    {
        fromBuilder!.UsingKey(keyAccessor);
        return this;
    }

    /// <inheritdoc/>
    public IAddChildBuilder<TChildModel, TEvent> UsingKeyFromContext(Expression<Func<TEvent, EventContext>> keyAccessor)
    {
        fromBuilder!.UsingKeyFromContext(keyAccessor);
        return this;
    }

    /// <inheritdoc/>
    public IAddChildBuilder<TChildModel, TEvent> UsingParentKey<TProperty>(Expression<Func<TEvent, TProperty>> keyAccessor)
    {
        fromBuilder!.UsingParentKey(keyAccessor);
        return this;
    }

    /// <inheritdoc/>
    public IAddChildBuilder<TChildModel, TEvent> UsingParentCompositeKey<TKeyType>(Action<ICompositeKeyBuilder<TKeyType, TEvent>> builderCallback)
    {
        fromBuilder!.UsingParentCompositeKey(builderCallback);
        return this;
    }

    /// <inheritdoc/>
    public IAddChildBuilder<TChildModel, TEvent> UsingParentKeyFromContext<TProperty>(Expression<Func<TEvent, TProperty>> keyAccessor)
    {
        fromBuilder!.UsingParentKeyFromContext(keyAccessor);
        return this;
    }

    /// <inheritdoc/>
    public IAddChildBuilder<TChildModel, TEvent> IdentifiedBy<TProperty>(Expression<Func<TChildModel, TProperty>> propertyExpression)
    {
        childrenBuilder.IdentifiedBy(propertyExpression);
        return this;
    }

    /// <inheritdoc/>
    public IAddChildBuilder<TChildModel, TEvent> FromObject(Expression<Func<TEvent, TChildModel>> propertyWithChild)
    {
        var childProperty = propertyWithChild.GetPropertyPath();
        foreach (var property in typeof(TChildModel).GetProperties())
        {
            var propertyPath = new PropertyPath(property.Name);
            fromBuilder!.Set(propertyPath).To(childProperty + propertyPath);
        }

        return this;
    }
}
