// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;

namespace Cratis.Conversion;

/// <summary>
/// Represents known type converters in the system.
/// </summary>
public static class TypeConverters
{
    /// <summary>
    /// Register all type converters.
    /// </summary>
    public static void Register()
    {
        TypeDescriptor.AddAttributes(typeof(DateOnly), new TypeConverterAttribute(typeof(DateOnlyTypeConverter)));
        TypeDescriptor.AddAttributes(typeof(TimeOnly), new TypeConverterAttribute(typeof(TimeOnlyTypeConverter)));
    }
}
