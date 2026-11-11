// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using CollectionProperty = (System.Func<object, object?> Get, System.Action<object, object?> Set, System.Func<object> CreateEmpty);

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Serialization semantics a read model is read back with.
/// </summary>
/// <remarks>
/// The read-model sink omits an empty child collection rather than writing <c language="csharp">[]</c>, deliberately: a parallel
/// replay races sibling partitions through their own events, and an unconstrained <c language="csharp">Children=[]</c> from the
/// root's own event would erase whatever a sibling's <c language="csharp">ChildAdded</c> already pushed. Absence is the encoding,
/// and it is not going to change.
/// <para>
/// What must change is what a reader does with that absence. A read model declares its child collection as a
/// non-nullable <see cref="IEnumerable{T}"/>, so nullable-reference analysis never warns and nothing prompts a
/// guard - and the property materializes as <see langword="null"/> anyway, on the state every one of these read
/// models is in immediately after creation. The declared type is a promise the deserializer was not keeping.
/// </para>
/// </remarks>
public static class ReadModelJsonSerialization
{
    static readonly Dictionary<Type, Func<object>?> _emptyFactories = [];
    static readonly Lock _emptyFactoriesLock = new();

    /// <summary>
    /// Add the read-model collection semantics to a set of <see cref="JsonSerializerOptions"/>.
    /// </summary>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to add to.</param>
    /// <returns>The <paramref name="options"/> for continuation.</returns>
    /// <remarks>
    /// Applied to the client's own options and to the ones the in-process spec harness materializes through, so
    /// that a spec and a running system answer "what is an empty child collection" with the same code rather than
    /// with two constants that happen to match.
    /// </remarks>
    public static JsonSerializerOptions WithDeclaredCollectionsNeverNull(this JsonSerializerOptions options)
    {
        options.TypeInfoResolver = (options.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver())
            .WithAddedModifier(DeclaredCollectionsAreNeverNull);
        return options;
    }

    /// <summary>
    /// A <see cref="JsonTypeInfo"/> modifier that materializes a property declared as a non-nullable collection
    /// as an empty collection when the document carries no value for it.
    /// </summary>
    /// <param name="typeInfo">The <see cref="JsonTypeInfo"/> to modify.</param>
    /// <remarks>
    /// Only non-nullable declarations are touched. A read model that genuinely needs to tell "no collection" from
    /// "an empty one" says so by declaring the property nullable, and then gets to keep the distinction - the same
    /// carve-out the TypeScript reader made when it was fixed for this in Fundamentals 7.16.8.
    /// </remarks>
    public static void DeclaredCollectionsAreNeverNull(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
        {
            return;
        }

        var properties = FindNeverNullCollectionProperties(typeInfo);
        if (properties.Count == 0)
        {
            return;
        }

        var previous = typeInfo.OnDeserialized;
        typeInfo.OnDeserialized = instance =>
        {
            previous?.Invoke(instance);
            MaterializeEmptyCollections(instance, properties);
        };
    }

    static List<CollectionProperty> FindNeverNullCollectionProperties(JsonTypeInfo typeInfo)
    {
        var nullabilityContext = new NullabilityInfoContext();
        List<CollectionProperty> properties = [];

        foreach (var property in typeInfo.Properties)
        {
            if (property is not { Get: { } get, Set: { } set })
            {
                continue;
            }

            if (GetEmptyFactoryForNonNullable(property, nullabilityContext) is { } createEmpty)
            {
                properties.Add((get, set, createEmpty));
            }
        }

        return properties;
    }

    static void MaterializeEmptyCollections(object instance, List<CollectionProperty> properties)
    {
        foreach (var (get, set, createEmpty) in properties)
        {
            if (get(instance) is null)
            {
                set(instance, createEmpty());
            }
        }
    }

    static Func<object>? GetEmptyFactoryForNonNullable(JsonPropertyInfo property, NullabilityInfoContext nullabilityContext) =>
        IsNonNullableDeclaration(property, nullabilityContext) ? GetEmptyFactory(property.PropertyType) : null;

    static bool IsNonNullableDeclaration(JsonPropertyInfo property, NullabilityInfoContext nullabilityContext) =>
        property.AttributeProvider switch
        {
            PropertyInfo propertyInfo => nullabilityContext.Create(propertyInfo).ReadState == NullabilityState.NotNull,
            FieldInfo fieldInfo => nullabilityContext.Create(fieldInfo).ReadState == NullabilityState.NotNull,
            _ => false
        };

    static Func<object>? GetEmptyFactory(Type type)
    {
        lock (_emptyFactoriesLock)
        {
            if (_emptyFactories.TryGetValue(type, out var cached))
            {
                return cached;
            }

            var factory = CreateEmptyFactory(type);
            _emptyFactories[type] = factory;
            return factory;
        }
    }

    static Func<object>? CreateEmptyFactory(Type type)
    {
        if (!IsEmptyFactoryCandidate(type))
        {
            return null;
        }

        if (type.IsArray)
        {
            var elementType = type.GetElementType()!;
            return () => Array.CreateInstance(elementType, 0);
        }

        var enumerableInterface = FindEnumerableInterface(type);
        return enumerableInterface is null ? null : CreateCollectionFactory(type, enumerableInterface.GetGenericArguments()[0]);
    }

    static bool IsEmptyFactoryCandidate(Type type) =>
        // A string is an IEnumerable and a dictionary's absence is a different question with a different answer,
        // so neither is in scope here.
        type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type) && !IsDictionary(type);

    static Type? FindEnumerableInterface(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            ? type
            : Array.Find(type.GetInterfaces(), _ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IEnumerable<>));

    static Func<object>? CreateCollectionFactory(Type type, Type itemType)
    {
        foreach (var candidate in new[] { typeof(List<>), typeof(HashSet<>) })
        {
            var concrete = candidate.MakeGenericType(itemType);
            if (type.IsAssignableFrom(concrete))
            {
                return () => Activator.CreateInstance(concrete)!;
            }
        }

        // Anything the reader cannot construct - a custom collection with no parameterless constructor, say -
        // is left as it was rather than guessed at.
        return null;
    }

    static bool IsDictionary(Type type) =>
        Array.Exists(
            type.GetInterfaces(),
            _ => _ == typeof(IDictionary) ||
                 (_.IsGenericType &&
                  (_.GetGenericTypeDefinition() == typeof(IDictionary<,>) ||
                   _.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>))));
}
