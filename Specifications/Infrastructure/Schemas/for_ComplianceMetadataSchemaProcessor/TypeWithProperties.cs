// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Aksio.Cratis.Schemas.for_ComplianceMetadataSchemaProcessor;

public class TypeWithProperties
{
    public string First { get; set; }
    public string Second { get; set; }

    public static PropertyInfo FirstProperty = typeof(TypeWithProperties).GetProperty(nameof(First), BindingFlags.Public | BindingFlags.Instance);
    public static PropertyInfo SecondProperty = typeof(TypeWithProperties).GetProperty(nameof(Second), BindingFlags.Public | BindingFlags.Instance);
}
