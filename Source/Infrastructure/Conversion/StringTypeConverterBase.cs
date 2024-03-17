// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Globalization;

namespace Cratis.Conversion;

/// <summary>
/// Defines a base type for dealing with conversion of specific types to and from string.
/// </summary>
/// <typeparam name="T">Target type.</typeparam>
public abstract class StringTypeConverterBase<T> : TypeConverter
{
    /// <summary>
    /// Parse string to the specific type.
    /// </summary>
    /// <param name="source">String to parse.</param>
    /// <returns>Instance of the converted type.</returns>
    public abstract T Parse(string source);

    /// <summary>
    /// Convert to string from the specific type instance.
    /// </summary>
    /// <param name="source">Instance of type to convert.</param>
    /// <returns>String representation.</returns>
    public abstract string ToString(T source);

    /// <inheritdoc/>
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        if (sourceType == typeof(string))
        {
            return true;
        }
        return base.CanConvertFrom(context, sourceType);
    }

    /// <inheritdoc/>
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string str)
        {
            return Parse(str);
        }
        return base.ConvertFrom(context, culture, value);
    }

    /// <inheritdoc/>
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        if (destinationType == typeof(string))
        {
            return true;
        }
        return base.CanConvertTo(context, destinationType);
    }

    /// <inheritdoc/>
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is T typedValue)
        {
            return ToString(typedValue);
        }
        return base.ConvertTo(context, culture, value, destinationType);
    }
}
