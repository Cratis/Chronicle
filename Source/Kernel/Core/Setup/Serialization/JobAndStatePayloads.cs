// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Finds the types a grain persists as its state and the types a job step is declared with, which cross silos or storage
/// without being the parameter or return type of a grain method.
/// </summary>
/// <remarks>
/// The payload manifest follows the grain interfaces, so it never sees these: the request and result a job step is declared
/// with, travelling inside the generic job interfaces and a result union, and the state a grain keeps. A type among them that
/// has no generated Orleans serializer is carried by the JSON codec, as every Cratis type was before kernel payloads moved to
/// generated serializers; leaving it without one fails the first time it reaches another silo.
/// </remarks>
internal static class JobAndStatePayloads
{
    static readonly Lazy<HashSet<Type>> _types = new(Find);

    /// <summary>
    /// Gets every type a grain persists or a job step is declared with, whether or not it has a generated serializer.
    /// </summary>
    public static IReadOnlySet<Type> All => _types.Value;

    /// <summary>
    /// Check whether a type is one a grain persists or a job step is declared with.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to check.</param>
    /// <returns>True if it is; false if not.</returns>
    public static bool Includes(Type type) => _types.Value.Contains(type) && !type.IsDefined(typeof(GenerateSerializerAttribute), inherit: false);

    static HashSet<Type> Find()
    {
        var types = new HashSet<Type>();
        var assembly = typeof(JobAndStatePayloads).Assembly;
        foreach (var type in assembly.GetTypes().Where(_ => _.IsClass && !_.IsAbstract))
        {
            if (typeof(Grain).IsAssignableFrom(type))
            {
                foreach (var constructor in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    foreach (var parameter in constructor.GetParameters().Where(_ => _.ParameterType.IsGenericType && _.ParameterType.GetGenericTypeDefinition() == typeof(IPersistentState<>)))
                    {
                        Add(types, parameter.ParameterType.GetGenericArguments()[0]);
                    }
                }
            }

            for (var current = type.BaseType; current is not null; current = current.BaseType)
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition().Name.StartsWith("JobStep`3", StringComparison.Ordinal))
                {
                    foreach (var argument in current.GetGenericArguments())
                    {
                        Add(types, argument);
                    }

                    break;
                }
            }
        }

        return types;
    }

    static void Add(HashSet<Type> types, Type type)
    {
        if (type.IsGenericParameter || type == typeof(object))
        {
            return;
        }

        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments())
            {
                Add(types, argument);
            }
        }

        if (type.Namespace?.StartsWith("Cratis.Chronicle", StringComparison.Ordinal) == true)
        {
            types.Add(type);
        }
    }
}
