// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

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
    }
}
