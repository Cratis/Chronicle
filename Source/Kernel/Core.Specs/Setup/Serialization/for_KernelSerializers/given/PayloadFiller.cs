// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Dynamic;
using System.Net;
using System.Reflection;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Setup.Serialization.for_KernelSerializers.given;

/// <summary>
/// Builds a fully populated instance of a payload type, so a serializer that forgets a member shows up as a difference.
/// </summary>
/// <param name="lazySequences">Whether sequences are lazy iterators, the way a query result is, instead of lists.</param>
public class PayloadFiller(bool lazySequences = false)
{
    const int MaxDepth = 5;
    int _counter;

    /// <summary>
    /// Create a populated instance.
    /// </summary>
    /// <param name="type">Type to create.</param>
    /// <returns>The populated instance.</returns>
    public object? Create(Type type) => Create(type, 0);

    static object? CreateSimple(Type type, int next)
    {
        if (type == typeof(string))
        {
            return $"value-{next}";
        }
        if (type == typeof(Guid))
        {
            return new Guid(next, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        }
        if (type == typeof(bool))
        {
            return true;
        }
        if (type == typeof(DateTimeOffset))
        {
            return new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero).AddMinutes(next);
        }
        if (type == typeof(DateTime))
        {
            return new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc).AddMinutes(next);
        }
        if (type == typeof(TimeSpan))
        {
            return TimeSpan.FromSeconds(next);
        }
        if (type == typeof(DateOnly))
        {
            return new DateOnly(2026, 1, 2).AddDays(next);
        }
        if (type == typeof(TimeOnly))
        {
            return new TimeOnly(3, 4, 5).AddMinutes(next);
        }
        if (type == typeof(Uri))
        {
            return new Uri($"https://example.com/{next}");
        }
        if (type == typeof(byte[]))
        {
            return new byte[] { 1, 2, 3, (byte)next };
        }
        if (type.IsEnum)
        {
            var values = Enum.GetValues(type);
            return values.GetValue(values.Length > 1 ? 1 : 0);
        }
        if (type.IsPrimitive || type == typeof(decimal))
        {
            return Convert.ChangeType(next + 1, type);
        }

        return null;
    }

    static bool IsConcept(Type type)
    {
        for (var current = type; current is not null && current != typeof(object); current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition().Name.StartsWith("ConceptAs", StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    static IEnumerable<T> AsLazySequence<T>(List<T> items) => items.Select(_ => _);

    object? Create(Type type, int depth)
    {
        var next = ++_counter;
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying is not null)
        {
            return Create(underlying, depth);
        }

        var simple = CreateSimple(type, next);
        if (simple is not null)
        {
            return simple;
        }

        if (typeof(JsonNode).IsAssignableFrom(type))
        {
            return new JsonObject { ["name"] = $"value-{next}", ["count"] = next, ["nested"] = new JsonObject { ["flag"] = true }, ["list"] = new JsonArray(1, 2, 3) };
        }

        if (type == typeof(JsonSchema))
        {
            return new JsonSchema
            {
                Type = JsonObjectType.Object,
                Title = $"schema-{next}",
                Properties =
                {
                    ["name"] = new JsonSchemaProperty { Type = JsonObjectType.String },
                    ["count"] = new JsonSchemaProperty { Type = JsonObjectType.Integer }
                }
            };
        }

        if (type == typeof(ExpandoObject))
        {
            var expando = new ExpandoObject();
            var values = (IDictionary<string, object?>)expando;
            values["name"] = $"value-{next}";
            values["count"] = next;
            return expando;
        }

        if (type == typeof(object))
        {
            return $"value-{next}";
        }

        if (type == typeof(Type))
        {
            return typeof(PayloadFiller);
        }

        if (type == typeof(SiloAddress))
        {
            return SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111 + next), next);
        }

        if (type.IsArray)
        {
            var element = type.GetElementType()!;
            var array = Array.CreateInstance(element, depth >= MaxDepth ? 0 : 2);
            for (var i = 0; i < array.Length; i++)
            {
                array.SetValue(Create(element, depth + 1), i);
            }
            return array;
        }

        if (type.IsGenericType)
        {
            var collection = CreateCollection(type, depth);
            if (collection is not null)
            {
                return collection;
            }
        }

        if (depth > MaxDepth + 1 && !type.IsValueType)
        {
            // Recursive structures stop here with an empty instance where one can be made.
            return type.IsAbstract || type.IsInterface ? null : type.GetConstructor(Type.EmptyTypes)?.Invoke(null);
        }

        if (type.IsInterface || type.IsAbstract)
        {
            var concrete = type.Assembly.GetTypes()
                .Where(_ => !_.IsAbstract && !_.IsInterface && !_.IsGenericTypeDefinition && type.IsAssignableFrom(_) && _.IsPublic)
                .OrderBy(_ => _.FullName, StringComparer.Ordinal)
                .FirstOrDefault();
            return concrete is null ? null : Create(concrete, depth);
        }

        return CreateComplex(type, depth);
    }

    object? CreateCollection(Type type, int depth)
    {
        var definition = type.GetGenericTypeDefinition();
        var arguments = type.GetGenericArguments();
        var count = depth >= MaxDepth ? 0 : 2;

        if (arguments.Length == 2 && definition.Name.Contains("Dictionary"))
        {
            var concrete = typeof(Dictionary<,>).MakeGenericType(arguments);
            var dictionary = (IDictionary)Activator.CreateInstance(concrete)!;
            for (var i = 0; i < count; i++)
            {
                dictionary[Create(arguments[0], depth + 1)!] = Create(arguments[1], depth + 1);
            }

            if (type.IsAssignableFrom(concrete))
            {
                return dictionary;
            }

            if (definition == typeof(FrozenDictionary<,>))
            {
                return typeof(FrozenDictionary).GetMethods().First(_ => _.Name == "ToFrozenDictionary" && _.GetParameters().Length == 2)
                    .MakeGenericMethod(arguments).Invoke(null, [dictionary, null]);
            }

            return null;
        }

        if (arguments.Length == 1)
        {
            var item = arguments[0];
            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(item))!;
            for (var i = 0; i < count; i++)
            {
                list.Add(Create(item, depth + 1));
            }

            if (lazySequences && definition == typeof(IEnumerable<>))
            {
                return typeof(PayloadFiller).GetMethod(nameof(AsLazySequence), BindingFlags.Static | BindingFlags.NonPublic)!.MakeGenericMethod(item).Invoke(null, [list]);
            }

            if (type.IsAssignableFrom(list.GetType()))
            {
                return list;
            }

            if (definition == typeof(ImmutableArray<>))
            {
                return typeof(ImmutableArray).GetMethods().First(_ => _.Name == "CreateRange" && _.GetParameters().Length == 1 && _.GetParameters()[0].ParameterType.IsGenericType)
                    .MakeGenericMethod(item).Invoke(null, [list]);
            }

            var set = Activator.CreateInstance(typeof(HashSet<>).MakeGenericType(item), list)!;
            if (type.IsAssignableFrom(set.GetType()))
            {
                return set;
            }

            if (definition == typeof(FrozenSet<>))
            {
                return typeof(FrozenSet).GetMethods().First(_ => _.Name == "ToFrozenSet" && _.GetParameters().Length == 2).MakeGenericMethod(item).Invoke(null, [set, null]);
            }

            if (definition == typeof(ImmutableList<>))
            {
                return typeof(ImmutableList).GetMethods().First(_ => _.Name == "CreateRange" && _.GetParameters().Length == 1).MakeGenericMethod(item).Invoke(null, [list]);
            }
        }

        return null;
    }

    object? CreateComplex(Type type, int depth)
    {
        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        var constructor = constructors
            .OrderByDescending(_ => depth >= MaxDepth ? -_.GetParameters().Length : _.GetParameters().Length)
            .FirstOrDefault();

        object instance;
        var parameters = constructor?.GetParameters() ?? [];
        if (constructor is null)
        {
            if (!type.IsValueType)
            {
                return null;
            }
            instance = Activator.CreateInstance(type)!;
        }
        else
        {
            instance = constructor.Invoke(parameters.Select(_ => Create(_.ParameterType, depth + 1)).ToArray());
        }

        if (IsConcept(type))
        {
            // A concept is its constructor value; setting Value afterwards would make the two disagree.
            return instance;
        }

        var names = parameters.Select(_ => _.Name!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0 || property.SetMethod is not { IsPublic: true } || names.Contains(property.Name))
            {
                continue;
            }
            property.SetValue(instance, Create(property.PropertyType, depth + 1));
        }

        return instance;
    }
}
