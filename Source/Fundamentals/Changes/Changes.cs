// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Dynamic;
using Cratis.Objects;
using Cratis.Properties;

namespace Cratis.Changes
{
    /// <summary>
    /// Represents changes that can be applied to a <see cref="Changeset{TSource, TTarget}"/>.
    /// </summary>
    public static class Changes
    {
        /// <summary>
        /// Applies properties to the <see cref="Changeset{TSource, TTarget}"/>.
        /// </summary>
        /// <typeparam name="TSource">Source type for the changeset.</typeparam>
        /// <typeparam name="TTarget">Target type for the changeset</typeparam>
        /// <param name="changeset"><see cref="Changeset{TSource, TTarget}"/> to apply to.</param>
        /// <param name="propertyMappers">Collection of <see cref="PropertyMapper{TSource, TTarget}">property mappers</see> that will manipulate properties on the target.</param>
        /// <remarks>
        /// This will run a diff against the initial state and only produce changes that are new.
        /// </remarks>
        public static void ApplyProperties<TSource, TTarget>(this Changeset<TSource, TTarget> changeset, IEnumerable<PropertyMapper<TSource, TTarget>> propertyMappers)
        {
            var workingState = changeset.InitialState.Clone()!;
            foreach (var propertyMapper in propertyMappers)
            {
                propertyMapper(changeset.Incoming, workingState);
            }

            var comparer = new ObjectsComparer.Comparer<TTarget>();
            if (!comparer.Compare(changeset.InitialState, workingState, out var differences))
            {
                changeset.Add(new PropertiesChanged<TTarget>(workingState, differences.Select(_ => new PropertyDifference<TTarget>(changeset.InitialState, workingState, _))));
            }
        }

        /// <summary>
        /// Applies properties for a child to the <see cref="Changeset{TSource, TTarget}"/>.
        /// </summary>
        /// <typeparam name="TSource">Source type for the changeset.</typeparam>
        /// <typeparam name="TTarget">Target type for the changeset</typeparam>
        /// <typeparam name="TChild">Type of child.</typeparam>
        /// <param name="changeset"><see cref="Changeset{TSource, TTarget}"/> to apply to.</param>
        /// <param name="item">The item to add from.</param>
        /// <param name="childrenProperty">The <see cref="PropertyPath"/> on the parent that holds the children.</param>
        /// <param name="identifiedByProperty">The <see cref="PropertyPath"/> on the instance that identifies the child.</param>
        /// <param name="keyResolver">The <see cref="ValueProvider{T}"/> for resolving the key on the event.</param>
        /// <param name="propertyMappers">Collection of <see cref="PropertyMapper{TSource, TTarget}">property mappers</see> that will manipulate properties on the target.</param>
        /// <remarks>
        /// This will run a diff against the initial state and only produce changes that are new.
        /// </remarks>
        public static void ApplyChildProperties<TSource, TTarget, TChild>(
            this Changeset<TSource, TTarget> changeset,
            TChild item,
            PropertyPath childrenProperty,
            PropertyPath identifiedByProperty,
            ValueProvider<TSource> keyResolver,
            IEnumerable<PropertyMapper<TSource, TChild>> propertyMappers)
        {
            var workingItem = item.Clone()!;
            foreach (var propertyMapper in propertyMappers)
            {
                propertyMapper(changeset.Incoming, workingItem);
            }

            var comparer = new ObjectsComparer.Comparer();
            if (!comparer.Compare(item, workingItem, out var differences))
            {
                changeset.Add(new ChildPropertiesChanged<TChild>(
                    workingItem,
                    childrenProperty,
                    identifiedByProperty,
                    keyResolver(changeset.Incoming),
                    differences.Select(_ => new PropertyDifference<TChild>(item, workingItem, _))));
            }
        }

        /// <summary>
        /// Applies properties to the child in the model to the <see cref="Changeset{TSource, TTarget}"/>.
        /// </summary>
        /// <typeparam name="TSource">Source type for the changeset.</typeparam>
        /// <typeparam name="TTarget">Target type for the changeset</typeparam>
        /// <typeparam name="TChild">Type of child.</typeparam>
        /// <param name="changeset"><see cref="Changeset{TSource, TTarget}"/> to apply to.</param>
        /// <param name="childrenProperty"><see cref="PropertyPath"/> for accessing the children collection.</param>
        /// <param name="identifiedByProperty"><see cref="PropertyPath"/> that identifies the child.</param>
        /// <param name="key">Key value.</param>
        /// <param name="propertyMappers">Collection of <see cref="PropertyMapper{TSource, TTarget}">property mappers</see> that will manipulate properties on the target.</param>
        /// <exception cref="ChildrenPropertyIsNotEnumerable">Thrown when children property is not enumerable.</exception>
        public static void ApplyAddChild<TSource, TTarget, TChild>(
            this Changeset<TSource, TTarget> changeset,
            PropertyPath childrenProperty,
            PropertyPath identifiedByProperty,
            object key,
            IEnumerable<PropertyMapper<TSource, TChild>> propertyMappers)
            where TChild : new()
        {
            var workingState = changeset.InitialState.Clone()!;
            var items = workingState.EnsureCollection<TTarget, TChild>(childrenProperty);

            if (!items.Contains(identifiedByProperty, key))
            {
                var item = new TChild();

                foreach (var propertyMapper in propertyMappers)
                {
                    propertyMapper(changeset.Incoming, item);
                }

                identifiedByProperty.SetValue(item, key);
                ((IList<TChild>)items).Add(item);

                changeset.Add(new ChildAdded(item, childrenProperty, identifiedByProperty, key!));
            }
        }

        /// <summary>
        /// Apply a remove change to the <see cref="Changeset{TSource, TTarget}"/>.
        /// </summary>
        /// <typeparam name="TSource">Source type for the changeset.</typeparam>
        /// <typeparam name="TTarget">Target type for the changeset</typeparam>
        /// <param name="changeset"><see cref="Changeset{TSource, TTarget}"/> to apply to.</param>
        public static void ApplyRemove<TSource, TTarget>(this Changeset<TSource, TTarget> changeset)
        {
        }

        /// <summary>
        /// Apply a remove child change to the <see cref="Changeset{TSource, TTarget}"/>.
        /// </summary>
        /// <typeparam name="TSource">Source type for the changeset.</typeparam>
        /// <typeparam name="TTarget">Target type for the changeset</typeparam>
        /// <param name="changeset"><see cref="Changeset{TSource, TTarget}"/> to apply to.</param>
        public static void ApplyRemoveChild<TSource, TTarget>(this Changeset<TSource, TTarget> changeset)
        {
        }
    }
}
