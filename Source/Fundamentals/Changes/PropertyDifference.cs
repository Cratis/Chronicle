// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Aksio.Cratis.Concepts;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Changes
{
    /// <summary>
    /// Represents a value difference in a property of an object.
    /// </summary>
    /// <typeparam name="TTarget">Target type the property difference is for.</typeparam>
    public class PropertyDifference<TTarget>
    {
        readonly TTarget _initialInstance;
        readonly TTarget _modifiedInstance;

        /// <summary>
        /// Gets the full member path to the property that has changed.
        /// </summary>
        public PropertyPath PropertyPath { get; }

        /// <summary>
        /// Gets the original value - possibly default.
        /// </summary>
        public object? Original { get; }

        /// <summary>
        /// Gets the changed value - possibly default.
        /// </summary>
        public object? Changed { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyDifference{T}"/> class.
        /// </summary>
        /// <param name="propertyPath">Raw difference.</param>
        /// <param name="initialInstance">Original state.</param>
        /// <param name="modifiedInstance">Changed state.</param>
        public PropertyDifference(PropertyPath propertyPath, TTarget initialInstance, TTarget modifiedInstance)
        {
            _initialInstance = initialInstance;
            _modifiedInstance = modifiedInstance;
            PropertyPath = propertyPath;

            var original = GetValueFrom(_initialInstance);
            var changed = GetValueFrom(_modifiedInstance);

            var valueType = GetValueTypeFrom(original, changed);
            if (valueType != default)
            {
                Original = GetValueInActualType(valueType, original);
                Changed = GetValueInActualType(valueType, changed);
            }
        }

        object? GetValueFrom(TTarget instance)
        {
            object? value = null;

            if (instance is not null && PropertyPath.HasValue(instance, ArrayIndexer.NoIndexers))
            {
                value = PropertyPath.GetValue(instance, ArrayIndexer.NoIndexers);
            }

            return value;
        }

        object? GetValueInActualType(Type valueType, object? value)
        {
            if (value is not null || (value is string && string.IsNullOrEmpty(value as string)))
            {
                if (valueType.IsConcept())
                {
                    value = ConceptFactory.CreateConceptInstance(valueType, value);
                }
                else
                {
                    value = Convert.ChangeType(value, valueType, CultureInfo.InvariantCulture);
                }
            }
            return value;
        }

        Type? GetValueTypeFrom(object? originalValue, object? changedValue)
        {
            return originalValue?.GetType() ?? changedValue?.GetType() ?? default;
        }
    }
}
