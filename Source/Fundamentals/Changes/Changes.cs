// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Dynamic;
using Cratis.Properties;

namespace Cratis.Changes
{
    /// <summary>
    /// Represents changes that can be applied to a <see cref="Changeset{T}"/>.
    /// </summary>
    public static class Changes
    {
        /// <summary>
        /// Applies properties to the <see cref="Changeset{T}"/>.
        /// </summary>
        /// <typeparam name="T">Type the changeset is for.</typeparam>
        /// <param name="changeset"><see cref="Changeset{T}"/> to apply to.</param>
        /// <param name="propertyMappers">Collection of <see cref="PropertyMapper{T}">property mappers</see> that will manipulate properties on the target.</param>
        /// <remarks>
        /// This will run a diff against the initial state and only produce changes that are new.
        /// </remarks>
        public static void ApplyProperties<T>(this Changeset<T> changeset, IEnumerable<PropertyMapper<T>> propertyMappers)
        {
            var workingState = changeset.InitialState.Clone();
            foreach (var propertyMapper in propertyMappers)
            {
                propertyMapper(changeset.Incoming, workingState);
            }

            var comparer = new ObjectsComparer.Comparer<ExpandoObject>();
            if (!comparer.Compare(changeset.InitialState, workingState, out var differences))
            {
                changeset.Add(new PropertiesChanged(workingState, differences.Select(_ => new PropertyDifference(changeset.InitialState, workingState, _))));
            }
        }

        /// <summary>
        /// Applies properties for a child to the <see cref="Changeset{T}"/>.
        /// </summary>
        /// <typeparam name="T">Type the changeset is for.</typeparam>
        /// <param name="changeset"><see cref="Changeset{T}"/> to apply to.</param>
        /// <param name="item">The item to add from.</param>
        /// <param name="childrenProperty">The <see cref="Property"/> on the parent that holds the children.</param>
        /// <param name="identifiedByProperty">The <see cref="Property"/> on the instance that identifies the child.</param>
        /// <param name="keyResolver">The <see cref="ValueProvider{T}"/> for resolving the key on the event.</param>
        /// <param name="propertyMappers">Collection of <see cref="PropertyMapper{T}">property mappers</see> that will manipulate properties on the target.</param>
        /// <remarks>
        /// This will run a diff against the initial state and only produce changes that are new.
        /// </remarks>
        public static void ApplyChildProperties<T>(
            this Changeset<T> changeset,
            ExpandoObject item,
            Property childrenProperty,
            Property identifiedByProperty,
            ValueProvider<T> keyResolver,
            IEnumerable<PropertyMapper<T>> propertyMappers)
        {
            var workingItem = item.Clone();
            foreach (var propertyMapper in propertyMappers)
            {
                propertyMapper(changeset.Incoming, workingItem);
            }

            var comparer = new ObjectsComparer.Comparer<ExpandoObject>();
            if (!comparer.Compare(item, workingItem, out var differences))
            {
                changeset.Add(new ChildPropertiesChanged(
                    workingItem,
                    childrenProperty,
                    identifiedByProperty,
                    keyResolver(changeset.Incoming),
                    differences.Select(_ => new PropertyDifference(item, workingItem, _))));
            }
        }

        /// <summary>
        /// Applies properties to the child in the model to the <see cref="Changeset{T}"/>.
        /// </summary>
        /// <typeparam name="T">Type the changeset is for.</typeparam>
        /// <param name="changeset"><see cref="Changeset{T}"/> to apply to.</param>
        /// <param name="childrenProperty"><see cref="Property"/> for accessing the children collection.</param>
        /// <param name="identifiedByProperty"><see cref="Property"/> that identifies the child.</param>
        /// <param name="key">Key value.</param>
        /// <param name="propertyMappers">Collection of <see cref="PropertyMapper{T}">property mappers</see> that will manipulate properties on the target.</param>
        /// <exception cref="ChildrenPropertyIsNotEnumerable">Thrown when children property is not enumerable.</exception>
        public static void ApplyAddChild<T>(
            this Changeset<T> changeset,
            Property childrenProperty,
            Property identifiedByProperty,
            object key,
            IEnumerable<PropertyMapper<T>> propertyMappers)
        {
            var workingState = changeset.InitialState.Clone();
            var items = workingState.EnsureCollection(childrenProperty);

            if (!items.Contains(identifiedByProperty, key))
            {
                var item = new ExpandoObject();

                foreach (var propertyMapper in propertyMappers)
                {
                    propertyMapper(changeset.Incoming, item);
                }

                identifiedByProperty.SetValue(item, key);
                ((IList<ExpandoObject>)items).Add(item);

                changeset.Add(new ChildAdded(item, childrenProperty, identifiedByProperty, key!));
            }
        }

        /// <summary>
        /// Apply a remove change to the <see cref="Changeset{T}"/>.
        /// </summary>
        /// <typeparam name="T">Type the changeset is for.</typeparam>
        /// <param name="changeset"><see cref="Changeset{T}"/> to apply to.</param>
        public static void ApplyRemove<T>(this Changeset<T> changeset)
        {
        }

        /// <summary>
        /// Apply a remove child change to the <see cref="Changeset{T}"/>.
        /// </summary>
        /// <typeparam name="T">Type the changeset is for.</typeparam>
        /// <param name="changeset"><see cref="Changeset{T}"/> to apply to.</param>
        public static void ApplyRemoveChild<T>(this Changeset<T> changeset)
        {
        }
    }
}
