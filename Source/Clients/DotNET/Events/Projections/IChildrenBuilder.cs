// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Aksio.Cratis.Events.Projections.Definitions;

namespace Aksio.Cratis.Events.Projections
{
    /// <summary>
    /// Defines the builder for building out a child relationship on a model.
    /// </summary>
    /// <typeparam name="TParentModel">Parent model type.</typeparam>
    /// <typeparam name="TChildModel">Child model type.</typeparam>
    public interface IChildrenBuilder<TParentModel, TChildModel>
    {
        /// <summary>
        /// Start building the from expressions for a specific event type.
        /// </summary>
        /// <param name="builderCallback">Callback for building.</param>
        /// <typeparam name="TEvent">Type of event.</typeparam>
        /// <returns>Builder continuation.</returns>
        IChildrenBuilder<TParentModel, TChildModel> From<TEvent>(Action<IFromBuilder<TChildModel, TEvent>> builderCallback);

        /// <summary>
        /// Sets the property that identifies the child model in the collection within the parent.
        /// </summary>
        /// <param name="propertyExpression">The expression that represents the property used to identify.</param>
        /// <typeparam name="TProperty">Type of property.</typeparam>
        /// <returns>Builder continuation.</returns>
        IChildrenBuilder<TParentModel, TChildModel> IdentifiedBy<TProperty>(Expression<Func<TChildModel, TProperty>> propertyExpression);

        /// <summary>
        /// Defines what event removes a child. This is optional, your system can chose to not support removal.
        /// </summary>
        /// <typeparam name="TEvent">Type of event.</typeparam>
        /// <returns>Builder continuation.</returns>
        IChildrenBuilder<TParentModel, TChildModel> RemovedWith<TEvent>();

        /// <summary>
        /// Start building the children projection for a specific child model.
        /// </summary>
        /// <param name="targetProperty">Expression for expressing the target property.</param>
        /// <param name="builderCallback">Builder callback.</param>
        /// <typeparam name="TNestedChildModel">Type of nested child model.</typeparam>
        /// <returns>Builder continuation.</returns>
        IChildrenBuilder<TParentModel, TChildModel> Children<TNestedChildModel>(Expression<Func<TChildModel, IEnumerable<TChildModel>>> targetProperty, Action<IChildrenBuilder<TChildModel, TNestedChildModel>> builderCallback);

        /// <summary>
        /// Build the <see cref="ChildrenDefinition"/>.
        /// </summary>
        /// <returns>A a new <see cref="ChildrenDefinition"/>.</returns>
        ChildrenDefinition Build();
    }
}
