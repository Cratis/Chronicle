// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Concepts;

namespace Cratis.Dynamic
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
        /// Converts an object to a dynamic <see cref="ExpandoObject"/>
        /// </summary>
        /// <param name="original"></param>
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
                    var valueType = value.GetType();
                    if (!valueType.IsPrimitive &&
                        valueType != typeof(string) &&
                        valueType != typeof(Guid) &&
                        !valueType.IsConcept())
                    {
                        value = value.AsExpandoObject();
                    }
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
    }
}
