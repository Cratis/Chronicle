// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Discovers the convention-based handler methods on a <see cref="IReadModelReactor"/>.
/// </summary>
/// <remarks>
/// A handler method is a public instance method named <c>Added</c>, <c>Modified</c> or <c>Removed</c>. Its
/// first parameter is the read model — a single instance or an <see cref="IEnumerable{T}"/> of instances.
/// </remarks>
public static class ReadModelReactorMethods
{
    static readonly ConcurrentDictionary<Type, IReadOnlyList<ReadModelReactorMethod>> _cache = [];

    static readonly Dictionary<string, ReadModelChangeType> _changeTypesByMethodName = new(StringComparer.Ordinal)
    {
        [nameof(ReadModelChangeType.Added)] = ReadModelChangeType.Added,
        [nameof(ReadModelChangeType.Modified)] = ReadModelChangeType.Modified,
        [nameof(ReadModelChangeType.Removed)] = ReadModelChangeType.Removed
    };

    /// <summary>
    /// Get all discovered handler methods for a <see cref="IReadModelReactor"/> type.
    /// </summary>
    /// <param name="reactorType">The reactor <see cref="Type"/> to discover methods for.</param>
    /// <returns>The collection of <see cref="ReadModelReactorMethod"/>.</returns>
    public static IReadOnlyList<ReadModelReactorMethod> GetFor(Type reactorType) =>
        _cache.GetOrAdd(reactorType, Build);

    /// <summary>
    /// Get the distinct read model types a <see cref="IReadModelReactor"/> reacts to.
    /// </summary>
    /// <param name="reactorType">The reactor <see cref="Type"/> to get read model types for.</param>
    /// <returns>The distinct read model <see cref="Type"/> instances.</returns>
    public static IEnumerable<Type> GetReadModelTypesFor(Type reactorType) =>
        GetFor(reactorType).Select(_ => _.ReadModelType).Distinct();

    static IReadOnlyList<ReadModelReactorMethod> Build(Type reactorType)
    {
        var methods = new List<ReadModelReactorMethod>();
        foreach (var method in reactorType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (method.IsSpecialName || !_changeTypesByMethodName.TryGetValue(method.Name, out var changeType))
            {
                continue;
            }

            var parameters = method.GetParameters();
            if (parameters.Length == 0)
            {
                continue;
            }

            var (readModelType, isCollection) = ResolveReadModelType(parameters[0].ParameterType);
            methods.Add(new ReadModelReactorMethod(changeType, method, readModelType, isCollection));
        }

        return methods;
    }

    static (Type ReadModelType, bool IsCollection) ResolveReadModelType(Type firstParameterType)
    {
        if (firstParameterType != typeof(string))
        {
            var enumerable = firstParameterType.IsGenericType && firstParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                ? firstParameterType
                : firstParameterType.GetInterfaces().FirstOrDefault(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            if (enumerable is not null)
            {
                return (enumerable.GetGenericArguments()[0], true);
            }
        }

        return (firstParameterType, false);
    }
}
