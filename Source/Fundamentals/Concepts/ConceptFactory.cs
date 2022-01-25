// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Aksio.Cratis.Reflection;

#nullable disable

namespace Aksio.Cratis.Concepts
{
    /// <summary>
    /// Factory to create an instance of a <see cref="ConceptAs{T}"/> from the Type and Underlying value.
    /// </summary>
    public static class ConceptFactory
    {
        /// <summary>
        /// Creates an instance of a <see cref="ConceptAs{T}"/> given the type and underlying value.
        /// </summary>
        /// <param name="type">Type of the ConceptAs to create.</param>
        /// <param name="value">Value to give to this instance.</param>
        /// <returns>An instance of a ConceptAs with the specified value.</returns>
        public static object CreateConceptInstance(Type type, object value)
        {
            ArgumentNullException.ThrowIfNull(value, nameof(value));

            var val = new object();

            var valueProperty = type.GetTypeInfo().GetProperty(nameof(ConceptAs<object>.Value));
            var genericArgumentType = GetPrimitiveTypeConceptIsBasedOn(type);

            if (genericArgumentType.IsGuid())
            {
                if (value.GetType().IsGuid())
                {
                    val = value;
                }
                else if (value.GetType().IsString())
                {
                    val = Guid.Parse(value.ToString());
                }
                else
                {
                    val = value.GetType() == typeof(byte[]) ? new Guid(value as byte[]) : (object)Guid.Empty;
                }
            }
            else if (genericArgumentType.IsString())
            {
                val = value ?? string.Empty;
            }
            else if (genericArgumentType.IsDate())
            {
                val = value ?? default(DateTime);
            }
            else if (genericArgumentType.IsDateTimeOffset())
            {
                val = value ?? default(DateTimeOffset);
            }
            else if (genericArgumentType.IsAPrimitiveType())
            {
                val = value ?? Activator.CreateInstance(valueProperty.PropertyType);
            }

            if (val.GetType() != genericArgumentType && !IsGuidFromString(genericArgumentType, val))
                val = Convert.ChangeType(val, genericArgumentType, null);

            var instance = Activator.CreateInstance(type, val);
            valueProperty.SetValue(instance, val, null);
            return instance;
        }

        static Type GetPrimitiveTypeConceptIsBasedOn(Type conceptType)
        {
            return ConceptMap.GetConceptValueType(conceptType);
        }

        static bool IsGuidFromString(Type genericArgumentType, object value)
        {
            return genericArgumentType == typeof(Guid) && value.GetType() == typeof(string);
        }
    }
}
