// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using Aksio.Cratis.Concepts;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Dynamic
{
    /// <summary>
    /// Extension methods for working with <see cref="ExpandoObject"/>.
    /// </summary>
    public static class ExpandoObjectExtensions
    {
        /// <summary>
        /// Deep clone an <see cref="ExpandoObject"/>.
        /// </summary>
        /// <param name="original">The original <see cref="ExpandoObject"/>.</param>
        /// <returns>A cloned <see cref="ExpandoObject"/>.</returns>
        /// <remarks>
        /// If any of the values represents complex objects, it will not clone these
        /// and create new fresh instances - it will just copy these across.
        /// </remarks>
        public static ExpandoObject Clone(this ExpandoObject original)
        {
            var clone = new ExpandoObject();

            var originalAsDictionary = original as IDictionary<string, object>;
            var cloneAsDictionary = clone as IDictionary<string, object>;

            foreach (var (key, value) in originalAsDictionary)
            {
                cloneAsDictionary.Add(key, value is ExpandoObject child ? child.Clone() : value);
            }

            return clone;
        }

        /// <summary>
        /// Converts an object to a dynamic <see cref="ExpandoObject"/>.
        /// </summary>
        /// <param name="original">Original object to convert.</param>
        /// <returns>A new <see cref="ExpandoObject"/> representing the given object.</returns>
        public static ExpandoObject AsExpandoObject(this object original)
        {
            var expando = new ExpandoObject();
            var expandoAsDictionary = expando as IDictionary<string, object>;

            foreach (var property in original.GetType().GetProperties())
            {
                var value = property.GetValue(original, null);
                if (value != null)
                {
                    value = GetActualValueFrom(value);
                }
                expandoAsDictionary[property.Name] = value!;
            }

            return expando;
        }

        /// <summary>
        /// Creates a clone of left and applies all content from right on top, overwriting any existing properties in left.
        /// </summary>
        /// <param name="left">The left <see cref="ExpandoObject"/>.</param>
        /// <param name="right">The right <see cref="ExpandoObject"/>.</param>
        /// <returns>A new <see cref="ExpandoObject"/>.</returns>
        public static ExpandoObject OverwriteWith(this ExpandoObject left, ExpandoObject right)
        {
            var result = left.Clone();
            var resultAsDictionary = result as IDictionary<string, object>;
            var rightAsDictionary = right as IDictionary<string, object>;

            foreach (var (key, value) in rightAsDictionary)
            {
                var rightValue = value;
                if (resultAsDictionary.ContainsKey(key) && resultAsDictionary[key] is ExpandoObject leftValueExpandoObject &&
                    value is ExpandoObject valueAsExpandoObject)
                {
                    rightValue = leftValueExpandoObject.OverwriteWith(valueAsExpandoObject);
                }
                else if (rightValue is ExpandoObject rightValueAsExpandoObject)
                {
                    rightValue = rightValueAsExpandoObject.Clone();
                }

                resultAsDictionary[key] = rightValue;
            }

            return result;
        }

        /// <summary>
        /// Ensure a specific path for a <see cref="PropertyPath"/> exists on an <see cref="ExpandoObject"/>..
        /// </summary>
        /// <param name="target">Target <see cref="ExpandoObject"/>.</param>
        /// <param name="property"><see cref="PropertyPath"/> to get or create for.</param>
        /// <param name="arrayIndexers">All <see cref="ArrayIndexer">array indexers</see>.</param>
        /// <returns><see cref="ExpandoObject"/> at property.</returns>
        /// <exception cref="UndefinedArrayIndexer">Thrown if a required array indexer is undefined.</exception>
        /// <exception cref="SegmentValueIsNotCollection">Thrown if a segment value should be expando object.</exception>
        public static ExpandoObject EnsurePath(this ExpandoObject target, PropertyPath property, params ArrayIndexer[] arrayIndexers)
        {
            var currentTarget = target as IDictionary<string, object>;
            var segments = property.Segments.ToArray();
            var currentPath = new PropertyPath(string.Empty);

            for (var propertyIndex = 0; propertyIndex < segments.Length - 1; propertyIndex++)
            {
                var segment = segments[propertyIndex];
                currentPath += segment;
                switch (segment)
                {
                    case PropertyName propertyName:
                        {
                            if (!currentTarget.ContainsKey(propertyName.Value))
                            {
                                var nested = new ExpandoObject();
                                currentTarget[segment.Value] = nested;
                                currentTarget = nested!;
                            }
                            else
                            {
                                currentTarget = ((ExpandoObject)currentTarget[segment.Value])!;
                            }
                        }
                        break;

                    case ArrayIndex arrayIndex:
                        {
                            var indexer = Array.Find(arrayIndexers, _ => _.ArrayProperty.Equals(currentPath));
                            if (indexer == default)
                            {
                                throw new UndefinedArrayIndexer(property, arrayIndex.Value);
                            }
                            IEnumerable<ExpandoObject> collection;
                            if (!currentTarget.ContainsKey(arrayIndex.Value))
                            {
                                collection = new List<ExpandoObject>();
                                currentTarget[segment.Value] = collection;
                            }
                            else
                            {
                                if (currentTarget[segment.Value] is not IEnumerable enumerable)
                                {
                                    throw new SegmentValueIsNotCollection(property, segment);
                                }
                                collection = ((IEnumerable)currentTarget[segment.Value]).OfType<ExpandoObject>().ToList();
                            }

                            var element = collection
                                .Cast<IDictionary<string, object>>()
                                .SingleOrDefault(_ => _.ContainsKey(indexer.IdentifierProperty.Path) && _[indexer.IdentifierProperty.Path] == indexer.Identifier);

                            if (element == default)
                            {
                                element = new ExpandoObject()!;
                                element[indexer.IdentifierProperty.Path] = indexer.Identifier;
                                currentTarget[segment.Value] = collection.Append((element as ExpandoObject)!).ToList();
                            }
                            currentTarget = (element as ExpandoObject)!;
                        }
                        break;
                }
            }

            return (currentTarget as ExpandoObject)!;
        }

        /// <summary>
        /// Ensures that a collection exists for a specific <see cref="PropertyPath"/>.
        /// </summary>
        /// <typeparam name="TChild">Type of child for the collection.</typeparam>
        /// <param name="target">Target <see cref="ExpandoObject"/>.</param>
        /// <param name="childrenProperty"><see cref="PropertyPath"/> to ensure collection for.</param>
        /// <param name="arrayIndexers">All <see cref="ArrayIndexer">array indexers</see>.</param>
        /// <returns>The ensured <see cref="ICollection{ExpandoObject}"/>.</returns>
        /// <exception cref="ChildrenPropertyIsNotEnumerable">Thrown if there is an existing property and it is not enumerable.</exception>
        public static ICollection<TChild> EnsureCollection<TChild>(this ExpandoObject target, PropertyPath childrenProperty, params ArrayIndexer[] arrayIndexers)
        {
            var inner = target.EnsurePath(childrenProperty, arrayIndexers) as IDictionary<string, object>;
            if (!inner.ContainsKey(childrenProperty.LastSegment.Value))
            {
                inner[childrenProperty.LastSegment.Value] = new List<TChild>();
            }

            if (!(inner[childrenProperty.LastSegment.Value] is IEnumerable))
            {
                throw new ChildrenPropertyIsNotEnumerable(childrenProperty);
            }

            var items = (inner[childrenProperty.LastSegment.Value] as IEnumerable)!.Cast<TChild>();
            if (items is not IList<TChild>)
            {
                items = new List<TChild>(items!);
            }
            inner[childrenProperty.LastSegment.Value] = items;
            return (items as ICollection<TChild>)!;
        }

        /// <summary>
        /// Check if there is an item with a specific key in a collection of <see cref="ExpandoObject"/> items.
        /// </summary>
        /// <param name="items">Items to check.</param>
        /// <param name="identityProperty"><see cref="PropertyPath"/> holding identity on each item.</param>
        /// <param name="key">The key value to check for.</param>
        /// <returns>True if there is an item, false if not.</returns>
        public static bool Contains(this IEnumerable<ExpandoObject> items, PropertyPath identityProperty, object key) =>
            items!.Any((IDictionary<string, object> _) => _.ContainsKey(identityProperty.Path) && _[identityProperty.Path].Equals(key));

        static object GetActualValueFrom(object value)
        {
            var valueType = value.GetType();
            if (!valueType.IsPrimitive &&
                valueType != typeof(string) &&
                valueType != typeof(Guid) &&
                !valueType.IsConcept())
            {
                if (value is IEnumerable enumerableValue)
                {
                    var list = new List<object>();

                    foreach (var element in enumerableValue)
                    {
                        list.Add(GetActualValueFrom(element));
                    }

                    value = list.ToArray();
                }
                else
                {
                    value = value.AsExpandoObject();
                }
            }

            return value;
        }
    }
}
