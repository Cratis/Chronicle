// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Globalization;

namespace Cratis.Concepts;

/// <summary>
/// Represents a <see cref="TypeConverter"/> for handling conversion of concept as represented as its value type.
/// </summary>
/// <typeparam name="TConcept">Type of concept.</typeparam>
/// <typeparam name="TValue">Type of value.</typeparam>
public class ConceptAsTypeConverter<TConcept, TValue> : TypeConverter
{
    /// <inheritdoc/>
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => sourceType == typeof(TValue) || sourceType == typeof(string);

    /// <inheritdoc/>
    public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        return ConceptFactory.CreateConceptInstance(typeof(TConcept), value);
    }
}
