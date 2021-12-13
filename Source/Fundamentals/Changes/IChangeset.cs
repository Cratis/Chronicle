// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Properties;

namespace Cratis.Changes
{
    /// <summary>
    /// Represents a changeset of changes that can occur to an object.
    /// </summary>
    /// <typeparam name="TSource">Type of the source object we are working from.</typeparam>
    /// <typeparam name="TTarget">Type of target object we are applying changes to.</typeparam>
    public interface IChangeset<TSource, TTarget>
    {
        /// <summary>
        /// Gets the <see cref="Incoming"/> the <see cref="Changeset{TSource, TTarget}"/> is for.
        /// </summary>
        TSource Incoming { get; }

        /// <summary>
        /// Gets the initial state of before changes in changeset occurred.
        /// </summary>
        TTarget InitialState { get; }

        /// <summary>
        /// Gets all the changes for the changeset.
        /// </summary>
        IEnumerable<Change> Changes { get; }

        /// <summary>
        /// Add a change to the changeset.
        /// </summary>
        /// <param name="change"><see cref="Change"/> to add.</param>
        void Add(Change change);

        /// <summary>
        /// Applies properties to the <see cref="Changeset{TSource, TTarget}"/>.
        /// </summary>
        /// <param name="propertyMappers">Collection of <see cref="PropertyMapper{TSource, TTarget}">property mappers</see> that will manipulate properties on the target.</param>
        /// <remarks>
        /// This will run a diff against the initial state and only produce changes that are new.
        /// </remarks>
        void SetProperties(IEnumerable<PropertyMapper<TSource, TTarget>> propertyMappers);

        /// <summary>
        /// Applies properties for a child to the <see cref="Changeset{TSource, TTarget}"/>.
        /// </summary>
        /// <typeparam name="TChild">Type of child.</typeparam>
        /// <param name="item">The item to add from.</param>
        /// <param name="childrenProperty">The <see cref="PropertyPath"/> on the parent that holds the children.</param>
        /// <param name="identifiedByProperty">The <see cref="PropertyPath"/> on the instance that identifies the child.</param>
        /// <param name="keyResolver">The <see cref="ValueProvider{T}"/> for resolving the key on the event.</param>
        /// <param name="propertyMappers">Collection of <see cref="PropertyMapper{TSource, TTarget}">property mappers</see> that will manipulate properties on the target.</param>
        /// <remarks>
        /// This will run a diff against the initial state and only produce changes that are new.
        /// </remarks>
        void SetChildProperties<TChild>(TChild item, PropertyPath childrenProperty, PropertyPath identifiedByProperty, ValueProvider<TSource> keyResolver, IEnumerable<PropertyMapper<TSource, TChild>> propertyMappers);

        /// <summary>
        /// Applies properties to the child in the model to the <see cref="Changeset{TSource, TTarget}"/>.
        /// </summary>
        /// <typeparam name="TChild">Type of child.</typeparam>
        /// <param name="childrenProperty"><see cref="PropertyPath"/> for accessing the children collection.</param>
        /// <param name="identifiedByProperty"><see cref="PropertyPath"/> that identifies the child.</param>
        /// <param name="key">Key value.</param>
        /// <param name="propertyMappers">Collection of <see cref="PropertyMapper{TSource, TTarget}">property mappers</see> that will manipulate properties on the target.</param>
        /// <exception cref="ChildrenPropertyIsNotEnumerable">Thrown when children property is not enumerable.</exception>
        void AddChild<TChild>(PropertyPath childrenProperty, PropertyPath identifiedByProperty, object key, IEnumerable<PropertyMapper<TSource, TChild>> propertyMappers) where TChild : new();

        /// <summary>
        /// Apply a remove change to the <see cref="Changeset{TSource, TTarget}"/>.
        /// </summary>
        void Remove();

        /// <summary>
        /// Apply a remove child change to the <see cref="Changeset{TSource, TTarget}"/>.
        /// </summary>
        void RemoveChild();
    }
}
