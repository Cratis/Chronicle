// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Keys;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Reflection helpers for discovering the shape of children collections and their item types.
/// </summary>
static class ChildTypeInspection
{
    const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

    /// <summary>
    /// Gets the public constructor with the most parameters, which is the primary constructor of a record.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>The constructor, or <see langword="null"/> when there is no public constructor.</returns>
    internal static ConstructorInfo? GetPrimaryConstructor(Type type) =>
        type.GetConstructors(PublicInstance)
            .OrderByDescending(constructor => constructor.GetParameters().Length)
            .FirstOrDefault();

    /// <summary>
    /// Reads a string-valued property from an attribute by name.
    /// </summary>
    /// <param name="attribute">The attribute to read from.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>The value, or <see langword="null"/> when the property is missing or not a string.</returns>
    internal static string? ReadString(this Attribute attribute, string propertyName) =>
        attribute.GetType().GetProperty(propertyName)?.GetValue(attribute) as string;

    /// <summary>
    /// Determines whether an attribute is an instance of the given generic attribute type definition.
    /// </summary>
    /// <param name="attribute">The attribute to check.</param>
    /// <param name="genericDefinition">The open generic attribute type.</param>
    /// <returns><see langword="true"/> if the attribute is a closed form of the definition; otherwise <see langword="false"/>.</returns>
    internal static bool IsClosedFormOf(this Attribute attribute, Type genericDefinition) =>
        attribute.GetType().IsGenericType && attribute.GetType().GetGenericTypeDefinition() == genericDefinition;

    /// <summary>
    /// Gets the item type of an enumerable member type.
    /// </summary>
    /// <param name="propertyType">The type of the member.</param>
    /// <returns>The item type, or <see langword="null"/> when the type is not enumerable.</returns>
    internal static Type? GetChildType(Type propertyType)
    {
        if (propertyType.IsGenericType && IsEnumerableDefinition(propertyType.GetGenericTypeDefinition()))
        {
            return propertyType.GetGenericArguments()[0];
        }

        return propertyType.GetInterfaces()
            .FirstOrDefault(IsEnumerableInterface)
            ?.GetGenericArguments()[0];
    }

    /// <summary>
    /// Resolves a children collection's declared item type to its single concrete <see cref="DerivedTypeAttribute"/>
    /// implementation, when it is a <see cref="DerivedTypeAttribute"/> base (an interface or abstract class) with
    /// exactly one registered implementation. Reflection-based discovery (constructor parameters for
    /// <c language="csharp">[SetFrom]</c>/<c language="csharp">[SetValue]</c>/<c language="csharp">[SetFromContext]</c>, and the <c language="csharp">[Key]</c> property for identity)
    /// finds nothing on an interface or abstract type, so without this resolution a polymorphic children
    /// collection silently loses those mappings. When the base type has zero or more than one implementation,
    /// the concrete type for this collection cannot be inferred safely, so the declared type is kept as-is.
    /// </summary>
    /// <param name="childType">The declared item type of the children collection.</param>
    /// <returns>The concrete implementation when unambiguous; otherwise <paramref name="childType"/> unchanged.</returns>
    internal static Type? ResolveConcreteChildType(Type? childType)
    {
        if (childType is null || (!childType.IsInterface && !childType.IsAbstract))
        {
            return childType;
        }

        var implementations = TypeUniverse.Current.All
            .Where(type => !type.IsAbstract && !type.IsInterface &&
                           childType.IsAssignableFrom(type) &&
                           Attribute.IsDefined(type, typeof(DerivedTypeAttribute)))
            .ToArray();

        return implementations.Length == 1 ? implementations[0] : childType;
    }

    /// <summary>
    /// Discovers the name of the property that identifies the items of a children collection.
    /// </summary>
    /// <param name="childType">The item type of the collection, if known.</param>
    /// <param name="childKey">The child key property name, used as the last resort.</param>
    /// <returns>The identifying property name, or <see cref="WellKnownExpressions.EventSourceId"/> when none is found.</returns>
    internal static string DiscoverKeyPropertyName(Type? childType, string childKey)
    {
        if (childType is null)
        {
            return WellKnownExpressions.EventSourceId;
        }

        var properties = childType.GetProperties(PublicInstance);

        return properties.FirstOrDefault(p => Attribute.IsDefined(p, typeof(KeyAttribute), true))?.Name
            ?? FindKeyConstructorParameterName(childType)
            ?? FindPropertyName(properties, "Id")
            ?? FindPropertyName(properties, childKey)
            ?? WellKnownExpressions.EventSourceId;
    }

    /// <summary>
    /// Infers the event property to use as the parent key for a <c language="csharp">[ChildrenFrom]</c> collection when no
    /// explicit <c language="csharp">parentKey</c> is supplied, by matching the parent read model's identifier type.
    /// </summary>
    /// <param name="eventType">The event type the children are built from.</param>
    /// <param name="parentModelType">The parent read model type, or <see langword="null"/> when unavailable.</param>
    /// <param name="childKey">The child key property name, excluded from the candidates.</param>
    /// <param name="namingPolicy">The <see cref="INamingPolicy"/> used to shape the resolved property name.</param>
    /// <returns>
    /// The inferred parent-key property name, or <see langword="null"/> when no candidate matches (the caller
    /// then falls back to <see cref="WellKnownExpressions.EventSourceId"/> for the same-event-source case).
    /// </returns>
    /// <remarks>
    /// The child <paramref name="childKey"/> property is excluded from the candidates — a property is the
    /// child's own key or the parent reference, never both. When more than one property still matches the
    /// parent identifier type the inference is ambiguous and the first by declaration order is used; supply
    /// an explicit <c language="csharp">parentKey</c> to disambiguate.
    /// </remarks>
    internal static string? DiscoverEventPropertyForParentId(Type eventType, Type? parentModelType, string childKey, INamingPolicy namingPolicy)
    {
        var parentIdType = parentModelType is null ? null : GetIdentifierType(parentModelType);
        if (parentIdType is null)
        {
            return null;
        }

        var match = Array.Find(
            eventType.GetProperties(PublicInstance),
            p => p.PropertyType == parentIdType && !p.Name.Equals(childKey, StringComparison.OrdinalIgnoreCase));

        return match is not null
            ? (string)ProjectionPropertyPaths.Resolve(match.Name, namingPolicy, ProjectionPathRole.Source)
            : null;
    }

    /// <summary>
    /// Determines whether the child type declares a <c language="csharp">[ChildrenFrom]</c> for the given event type.
    /// </summary>
    /// <param name="childType">The child type to inspect.</param>
    /// <param name="eventType">The event type to look for.</param>
    /// <returns><see langword="true"/> if a constructor parameter or property declares it; otherwise <see langword="false"/>.</returns>
    internal static bool HasChildrenFromForEventType(Type childType, Type eventType)
    {
        var parameters = GetPrimaryConstructor(childType)?.GetParameters() ?? [];
        return parameters.Any(parameter => DeclaresChildrenFrom(parameter.GetCustomAttributes(), eventType)) ||
               childType.GetProperties(PublicInstance).Any(property => DeclaresChildrenFrom(property.GetCustomAttributes(), eventType));
    }

    /// <summary>
    /// Gets the event types for which a type declares <c language="csharp">[ChildrenFrom]</c> collections.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>The event types, from the primary constructor parameters followed by the properties.</returns>
    internal static IEnumerable<Type> GetChildrenFromEventTypes(Type type)
    {
        var fromConstructor = GetPrimaryConstructor(type)?.GetParameters()
            .SelectMany(ChildrenFromEventTypesOf)
            ?? [];

        var fromProperties = type.GetProperties(PublicInstance)
            .SelectMany(ChildrenFromEventTypesOf);

        return fromConstructor.Concat(fromProperties);
    }

    static IEnumerable<Type> ChildrenFromEventTypesOf(ICustomAttributeProvider provider) =>
        provider.GetAttributesOfGenericType<ChildrenFromAttribute<object>>().Select(pair => pair.EventType);

    static bool DeclaresChildrenFrom(IEnumerable<Attribute> attributes, Type eventType) =>
        attributes
            .Any(attribute => attribute.IsClosedFormOf(typeof(ChildrenFromAttribute<>)) &&
                              attribute.GetType().GetGenericArguments()[0] == eventType);

    static bool IsEnumerableInterface(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>);

    static bool IsEnumerableDefinition(Type genericDefinition) =>
        genericDefinition == typeof(IEnumerable<>) || genericDefinition.GetInterfaces().Any(IsEnumerableInterface);

    static string? FindKeyConstructorParameterName(Type childType)
    {
        var keyParameter = childType.GetConstructors().FirstOrDefault()
            ?.GetParameters()
            .FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>(true) is not null);

        // Convert parameter name to property name (capitalize first letter)
        return keyParameter is null ? null : char.ToUpperInvariant(keyParameter.Name![0]) + keyParameter.Name.Substring(1);
    }

    static string? FindPropertyName(PropertyInfo[] properties, string name) =>
        properties.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Name;

    static Type? GetIdentifierType(Type modelType)
    {
        var idProperty = modelType.GetProperties(PublicInstance)
            .FirstOrDefault(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));
        if (idProperty is not null)
        {
            return idProperty.PropertyType;
        }

        return GetPrimaryConstructor(modelType)?.GetParameters()
            .FirstOrDefault(p => p.Name?.Equals("Id", StringComparison.OrdinalIgnoreCase) == true)
            ?.ParameterType;
    }
}
