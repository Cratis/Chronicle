// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Globalization;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Represents a <see cref="TypeConverter"/> for <see cref="ChronicleConnectionString"/>.
/// </summary>
public class ChronicleConnectionStringConverter : TypeConverter
{
    /// <inheritdoc/>
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    /// <inheritdoc/>
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string stringValue)
        {
            return new ChronicleConnectionString(stringValue);
        }

        return base.ConvertFrom(context, culture, value);
    }

    /// <inheritdoc/>
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }

    /// <inheritdoc/>
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (value is ChronicleConnectionString chronicleConnectionString && destinationType == typeof(string))
        {
            return chronicleConnectionString.ToString();
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }
}
