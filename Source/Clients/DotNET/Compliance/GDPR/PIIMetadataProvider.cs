// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Compliance.GDPR;

/// <summary>
/// Represents a metadata provider for PII.
/// </summary>
public class PIIMetadataProvider : ICanProvideComplianceMetadataForType, ICanProvideComplianceMetadataForProperty
{
    /// <inheritdoc/>
    public bool CanProvide(Type type)
    {
        if (!Attribute.IsDefined(type, typeof(PIIAttribute)))
        {
            return false;
        }

        ThrowIfEventSourceId(type);
        ThrowIfNotConceptAs(type);

        return true;
    }

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

    static void ThrowIfEventSourceId(Type type)
    {
        if (typeof(EventSourceId).IsAssignableFrom(type) || InheritsFromGenericEventSourceId(type))
        {
            throw new PIINotSupportedOnEventSourceId(type);
        }
    }

    static void ThrowIfNotConceptAs(Type type)
    {
        if (!InheritsFromConceptAs(type))
        {
            throw new PIIAppliedToNonConceptAsType(type);
        }
    }

    static bool InheritsFromConceptAs(Type type)
    {
        var current = type.BaseType;

        while (current is not null)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(ConceptAs<>))
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }

    static bool InheritsFromGenericEventSourceId(Type type)
    {
        var current = type.BaseType;

        while (current is not null)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(EventSourceId<>))
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }
}
