// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.ProtectedValues;

/// <summary>
/// Represents a metadata provider for plain-confidentiality <see cref="EncryptedAttribute"/> values.
/// </summary>
/// <remarks>
/// Mirrors <see cref="PIIMetadataProvider"/>'s shape - discovered the same way
/// (<see cref="ICanProvideComplianceMetadataForType"/> / <see cref="ICanProvideComplianceMetadataForProperty"/>),
/// rejecting an <see cref="EventSourceId"/>/<see cref="EventSourceId{T}"/> target for the same reason PII does -
/// but produces a different <see cref="ComplianceMetadataType"/> per <see cref="EncryptionScope"/>, so the
/// kernel dispatches a value marked <c language="csharp">[Encrypted]</c> to a handler that provisions a key under a
/// disjoint identity rather than the one PII uses for the same subject. Also refuses, at schema-generation time,
/// a property that resolves both <see cref="PIIAttribute"/> and <see cref="EncryptedAttribute"/> metadata - see
/// <see cref="PIIAndEncryptedCombinedNotSupported"/> for why that combination corrupts the value rather than
/// merely being redundant.
/// </remarks>
public class EncryptedMetadataProvider : ICanProvideComplianceMetadataForType, ICanProvideComplianceMetadataForProperty
{
    /// <inheritdoc/>
    public bool CanProvide(Type type)
    {
        if (!Attribute.IsDefined(type, typeof(EncryptedAttribute)))
        {
            return false;
        }

        ThrowIfEventSourceId(type);

        return true;
    }

    /// <inheritdoc/>
    public bool CanProvide(PropertyInfo property) =>
         Attribute.IsDefined(property, typeof(EncryptedAttribute)) ||
         (property.DeclaringType is not null && Attribute.IsDefined(property.DeclaringType, typeof(EncryptedAttribute))) ||
         CanProvide(property.PropertyType) ||
         HasAttributeOnConstructorParameter<EncryptedAttribute>(property);

    /// <inheritdoc/>
    public ComplianceMetadata Provide(Type type)
    {
        if (!CanProvide(type))
        {
            throw new NoComplianceMetadataForType(type);
        }

        var attribute = type.GetCustomAttribute<EncryptedAttribute>() ?? throw new NoComplianceMetadataForType(type);
        return new ComplianceMetadata(MetadataTypeFor(attribute.Scope), type.GetComplianceMetadataDetails());
    }

    /// <inheritdoc/>
    public ComplianceMetadata Provide(PropertyInfo property)
    {
        if (!CanProvide(property))
        {
            throw new NoComplianceMetadataForProperty(property);
        }

        ThrowIfAlsoPII(property);

        var attribute = ResolveAttribute(property) ?? throw new NoComplianceMetadataForProperty(property);
        return new ComplianceMetadata(MetadataTypeFor(attribute.Scope), property.GetComplianceMetadataDetails());
    }

    static ComplianceMetadataType MetadataTypeFor(EncryptionScope scope) => scope switch
    {
        EncryptionScope.Subject => ComplianceMetadataType.EncryptedSubject,
        EncryptionScope.Namespace => ComplianceMetadataType.EncryptedNamespace,
        EncryptionScope.Global => ComplianceMetadataType.EncryptedGlobal,
        _ => throw new EncryptionScopeNotYetSupported(scope)
    };

    static EncryptedAttribute? ResolveAttribute(PropertyInfo property) =>
        property.GetCustomAttribute<EncryptedAttribute>() ??
        property.DeclaringType?.GetCustomAttribute<EncryptedAttribute>() ??
        property.PropertyType.GetCustomAttribute<EncryptedAttribute>() ??
        ConstructorParameterAttribute<EncryptedAttribute>(property);

    static void ThrowIfAlsoPII(PropertyInfo property)
    {
        var hasPII =
            Attribute.IsDefined(property, typeof(PIIAttribute)) ||
            (property.DeclaringType is not null && Attribute.IsDefined(property.DeclaringType, typeof(PIIAttribute))) ||
            Attribute.IsDefined(property.PropertyType, typeof(PIIAttribute)) ||
            HasAttributeOnConstructorParameter<PIIAttribute>(property);

        if (hasPII)
        {
            throw new PIIAndEncryptedCombinedNotSupported(property.Name);
        }
    }

    static bool HasAttributeOnConstructorParameter<TAttribute>(PropertyInfo property)
        where TAttribute : Attribute => ConstructorParameterAttribute<TAttribute>(property) is not null;

    static TAttribute? ConstructorParameterAttribute<TAttribute>(PropertyInfo property)
        where TAttribute : Attribute
    {
        if (property.DeclaringType is null) return null;
        var ctor = property.DeclaringType.GetConstructors().MaxBy(c => c.GetParameters().Length);
        var param = ctor?.GetParameters().FirstOrDefault(p =>
            string.Equals(p.Name, property.Name, StringComparison.OrdinalIgnoreCase));

        return param?.GetCustomAttribute<TAttribute>();
    }

    static void ThrowIfEventSourceId(Type type)
    {
        if (typeof(EventSourceId).IsAssignableFrom(type) || InheritsFromGenericEventSourceId(type))
        {
            throw new EncryptedNotSupportedOnEventSourceId(type);
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
