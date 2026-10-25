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

        var details = type.GetComplianceMetadataDetails();
        return new ComplianceMetadata(ComplianceMetadataType.PII, string.IsNullOrEmpty(details) ? type.GetCustomAttribute<PIIAttribute>()?.Details ?? string.Empty : details);
    }

    /// <inheritdoc/>
    public ComplianceMetadata Provide(PropertyInfo property)
    {
        if (!CanProvide(property))
        {
            throw new NoComplianceMetadataForProperty(property);
        }

        var details = property.GetComplianceMetadataDetails();
        return new ComplianceMetadata(ComplianceMetadataType.PII, string.IsNullOrEmpty(details) ? PIIDetailsFor(property) : details);
    }

    static string PIIDetailsFor(PropertyInfo property) =>
        new[]
        {
            property.GetCustomAttribute<PIIAttribute>(),
            ConstructorParameterFor(property)?.GetCustomAttribute<PIIAttribute>(),
            property.DeclaringType?.GetCustomAttribute<PIIAttribute>(),
            property.PropertyType.GetCustomAttribute<PIIAttribute>()
        }.Select(attribute => attribute?.Details).FirstOrDefault(details => !string.IsNullOrEmpty(details)) ?? string.Empty;

    static bool HasPIIOnConstructorParameter(PropertyInfo property) =>
        ConstructorParameterFor(property) is { } parameter && Attribute.IsDefined(parameter, typeof(PIIAttribute));

    static ParameterInfo? ConstructorParameterFor(PropertyInfo property)
    {
        if (property.DeclaringType is null) return null;
        var ctor = property.DeclaringType.GetConstructors().MaxBy(c => c.GetParameters().Length);
        return ctor?.GetParameters().FirstOrDefault(p =>
            string.Equals(p.Name, property.Name, StringComparison.OrdinalIgnoreCase));
    }

    static void ThrowIfEventSourceId(Type type)
    {
        if (typeof(EventSourceId).IsAssignableFrom(type) || InheritsFromGenericEventSourceId(type))
        {
            throw new PIINotSupportedOnEventSourceId(type);
        }
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
