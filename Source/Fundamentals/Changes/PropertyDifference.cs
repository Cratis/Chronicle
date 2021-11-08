// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Globalization;
using System.Reflection;
using ObjectsComparer;

namespace Cratis.Changes
{
    /// <summary>
    /// Represents a value difference in a property of an object.
    /// </summary>
    public class PropertyDifference
    {
        readonly Difference _difference;
        readonly ExpandoObject _original;
        readonly ExpandoObject _changed;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyDifference"/> class.
        /// </summary>
        /// <param name="original">Original state.</param>
        /// <param name="changed">Changed state.</param>
        /// <param name="difference">Raw difference.</param>
        public PropertyDifference(ExpandoObject original, ExpandoObject changed, Difference difference)
        {
            _changed = changed;
            _original = original;
            _difference = difference;
            MemberPath = difference.MemberPath;

            var valueType = GetValueType();
            if (valueType != default)
            {
                if (!string.IsNullOrEmpty(difference.Value1))
                {
                    Original = Convert.ChangeType(difference.Value1, valueType, CultureInfo.InvariantCulture);
                }

                if (!string.IsNullOrEmpty(difference.Value2))
                {
                    Changed = Convert.ChangeType(difference.Value2, valueType, CultureInfo.InvariantCulture);
                }
            }
        }

        /// <summary>
        /// Gets the full member path to the property that has changed.
        /// </summary>
        public string MemberPath { get; }

        /// <summary>
        /// Gets the original value - possibly default.
        /// </summary>
        public object? Original { get; }

        /// <summary>
        /// Gets the changed value - possibly default.
        /// </summary>
        public object? Changed { get; }

        Type? GetValueType()
        {
            var originalValue = GetValueFrom(_original, _difference.MemberPath);
            var changedValue = GetValueFrom(_changed, _difference.MemberPath);
            return originalValue?.GetType() ?? changedValue?.GetType() ?? default;
        }

        object? GetValueFrom(ExpandoObject obj, string memberPath)
        {
            object? current = obj;

            foreach (var member in memberPath.Split("."))
            {
                if (obj is IDictionary<string, object> dictionary)
                {
                    if (dictionary.ContainsKey(member))
                    {
                        current = dictionary[member];
                    }
                    else
                    {
                        current = default;
                    }
                }
                else
                {
                    var property = obj.GetType().GetProperty(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    current = property?.GetValue(current) ?? default;
                }

                if (current == default) break;
            }

            return current;
        }
    }
}
