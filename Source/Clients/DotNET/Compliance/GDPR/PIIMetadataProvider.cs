// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Reflection;

namespace Cratis.Chronicle.Compliance.GDPR;

/// <summary>
/// Represents a metadata provider for PII.
/// </summary>
public class PIIMetadataProvider : ICanProvideComplianceMetadataForType, ICanProvideComplianceMetadataForProperty
{
    /// <inheritdoc/>
    public bool CanProvide(Type type) => type.Implements(typeof(IHoldPII)) || Attribute.IsDefined(type, typeof(PIIAttribute));

    /// <inheritdoc/>
    public bool CanProvide(PropertyInfo property) =>
         Attribute.IsDefined(property, typeof(PIIAttribute)) ||
         (property.DeclaringType is not null && Attribute.IsDefined(property.DeclaringType, typeof(PIIAttribute))) ||
         CanProvide(property.PropertyType) ||
         HasPIIOnConstructorParameter(property);

    /// <inheritdoc/>
    public ComplianceMetadata Provide(Type type)
    {
        if (!CanProvide(type))
        {
            throw new NoComplianceMetadataForType(type);
        }

        return new ComplianceMetadata(ComplianceMetadataType.PII, type.GetComplianceMetadataDetails());
    }

    /// <inheritdoc/>
    public ComplianceMetadata Provide(PropertyInfo property)
    {
        if (!CanProvide(property))
        {
            throw new NoComplianceMetadataForProperty(property);
        }

        return new ComplianceMetadata(ComplianceMetadataType.PII, property.GetComplianceMetadataDetails());
    }

    static bool HasPIIOnConstructorParameter(PropertyInfo property)
    {
        if (property.DeclaringType is null) return false;
        var ctor = property.DeclaringType.GetConstructors().MaxBy(c => c.GetParameters().Length);
        var param = ctor?.GetParameters().FirstOrDefault(p =>
            string.Equals(p.Name, property.Name, StringComparison.OrdinalIgnoreCase));
        return param is not null && Attribute.IsDefined(param, typeof(PIIAttribute));
    }
}
