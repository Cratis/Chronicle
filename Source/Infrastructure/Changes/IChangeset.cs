// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes;

/// <summary>
/// Represents a changeset of changes that can occur to an object.
/// </summary>
/// <typeparam name="TSource">Type of the source object we are working from.</typeparam>
/// <typeparam name="TTarget">Type of target object we are applying changes to.</typeparam>
public interface IChangeset<TSource, TTarget>
{
    /// <summary>
    /// Gets the <typeparamref name="TSource"/> the <see cref="IChangeset{TSource, TTarget}"/> is for.
    /// </summary>
    TSource Incoming { get; }

    /// <summary>
    /// Gets the initial state of before changes in changeset occurred.
    /// </summary>
    TTarget InitialState { get; set; }

    /// <summary>
    /// Gets the current state with all the changes in the changeset applied.
    /// </summary>
    TTarget CurrentState { get; }

    /// <summary>
    /// Gets all the changes for the changeset.
    /// </summary>
    IEnumerable<Change> Changes { get; }

    /// <summary>
    /// Gets whether there are changes in the changeset.
    /// </summary>
    bool HasChanges { get; }

    /// <summary>
    /// Add a change to the changeset.
    /// </summary>
    /// <param name="change"><see cref="Change"/> to add.</param>
    void Add(Change change);

    /// <summary>
    /// Applies properties to the <see cref="Changeset{TSource, TTarget}"/>.
    /// </summary>
    /// <param name="propertyMappers">Collection of <see cref="PropertyMapper{TSource, TTarget}">property mappers</see> that will manipulate properties on the target.</param>
    /// <param name="arrayIndexers"><see cref="ArrayIndexers"/> for accessing nested objects with arrays.</param>
    /// <remarks>
    /// This will run a diff against the initial state and only produce changes that are new.
    /// </remarks>
    void SetProperties(IEnumerable<PropertyMapper<TSource, TTarget>> propertyMappers, ArrayIndexers arrayIndexers);

    /// <summary>
    /// Apply a join change to the <see cref="Changeset{TSource, TTarget}"/>.
    /// </summary>
    /// <param name="onProperty">The property defining the property it was joined on.</param>
    /// <param name="key">Key representing the join.</param>
    /// <param name="arrayIndexers">All <see cref="ArrayIndexer">array indexers</see>.</param>
    /// <returns>A changeset that is scoped for the join.</returns>
    IChangeset<TSource, TTarget> Join(PropertyPath onProperty, object key, ArrayIndexers arrayIndexers);

    /// <summary>
    /// Apply a join resolution change to the <see cref="Changeset{TSource, TTarget}"/>.
    /// </summary>
    /// <param name="onProperty">The property defining the property it was joined on.</param>
    /// <param name="key">Key representing the join.</param>
    /// <param name="incoming">The incoming change that resolved the join.</param>
    /// <param name="arrayIndexers">All <see cref="ArrayIndexer">array indexers</see>.</param>
    /// <returns>A changeset that is scoped for the join.</returns>
    IChangeset<TSource, TTarget> ResolvedJoin(PropertyPath onProperty, object key, TSource incoming, ArrayIndexers arrayIndexers);

    /// <summary>
    /// Optimize the changeset.
    /// </summary>
    void Optimize();

    /// <summary>
    /// Adds a child as is to a given children property.
    /// </summary>
    /// <param name="childrenProperty"><see cref="PropertyPath"/> for accessing the children collection.</param>
    /// <param name="child">Child to add.</param>
    void AddChild(PropertyPath childrenProperty, object child);

    /// <summary>
    /// Applies properties to the child in the model to the <see cref="IChangeset{TSource, TTarget}"/>.
    /// </summary>
    /// <typeparam name="TChild">Type of child.</typeparam>
    /// <param name="childrenProperty"><see cref="PropertyPath"/> for accessing the children collection.</param>
    /// <param name="identifiedByProperty"><see cref="PropertyPath"/> that identifies the child.</param>
    /// <param name="key">Key value.</param>
    /// <param name="propertyMappers">Collection of <see cref="PropertyMapper{TSource, TTarget}">property mappers</see> that will manipulate properties on the target.</param>
    /// <param name="arrayIndexers">All <see cref="ArrayIndexer">array indexers</see>.</param>
    /// <exception cref="ChildrenPropertyIsNotEnumerable">Thrown when children property is not enumerable.</exception>
    void AddChild<TChild>(PropertyPath childrenProperty, PropertyPath identifiedByProperty, object key, IEnumerable<PropertyMapper<TSource, TTarget>> propertyMappers, ArrayIndexers arrayIndexers)
        where TChild : new();

    /// <summary>
    /// Apply a remove change to the <see cref="IChangeset{TSource, TTarget}"/>.
    /// </summary>
    void Remove();

    /// <summary>
    /// Apply a remove child change to the <see cref="IChangeset{TSource, TTarget}"/>.
    /// </summary>
    /// <param name="childrenProperty"><see cref="PropertyPath"/> for accessing the children collection.</param>
    /// <param name="identifiedByProperty"><see cref="PropertyPath"/> that identifies the child.</param>
    /// <param name="key">Key value.</param>
    /// <param name="arrayIndexers">All <see cref="ArrayIndexer">array indexers</see>.</param>
    void RemoveChild(PropertyPath childrenProperty, PropertyPath identifiedByProperty, object key, ArrayIndexers arrayIndexers);

    /// <summary>
    /// Apply a remove child change that will remove a specific child from all models that matches the identity to the <see cref="IChangeset{TSource, TTarget}"/>.
    /// </summary>
    /// <param name="childrenProperty"><see cref="PropertyPath"/> for accessing the children collection.</param>
    /// <param name="identifiedByProperty"><see cref="PropertyPath"/> that identifies the child.</param>
    /// <param name="key">Key value.</param>
    /// <param name="arrayIndexers">All <see cref="ArrayIndexer">array indexers</see>.</param>
    void RemoveChildFromAll(PropertyPath childrenProperty, PropertyPath identifiedByProperty, object key, ArrayIndexers arrayIndexers);

    /// <summary>
    /// Check if changeset contains a <see cref="ChildAdded"/> to a collection with a specific key.
    /// </summary>
    /// <param name="childrenProperty">The <see cref="PropertyPath"/> representing the collection.</param>
    /// <param name="key">The key of the item.</param>
    /// <returns>True if it has, false it not.</returns>
    bool HasChildBeenAddedWithKey(PropertyPath childrenProperty, object key);

    /// <summary>
    /// Check if there has been issued a remove on the changeset.
    /// </summary>
    /// <returns>True if there has, false if not.</returns>
    bool HasBeenRemoved();

    /// <summary>
    /// Checks if the changeset has a <see cref="Joined"/> or <see cref="ChildRemovedFromAll"/> change.
    /// </summary>
    /// <returns>True if it has, false if not.</returns>
    public bool HasJoined();

    /// <summary>
    /// Checks if the changeset has a <see cref="Removed"/> or <see cref="ChildRemoved"/> change.
    /// </summary>
    /// <returns>True if it has, false if not.</returns>
    public bool HasRemoved();

    /// <summary>
    /// Get a specific child from.
    /// </summary>
    /// <typeparam name="TChild">Type of child.</typeparam>
    /// <param name="key">The key of the item.</param>
    /// <returns>The added child.</returns>
    TChild GetChildByKey<TChild>(object key);
}
