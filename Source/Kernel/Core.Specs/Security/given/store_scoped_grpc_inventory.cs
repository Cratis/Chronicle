// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Security.given;

public static class store_scoped_grpc_inventory
{
    /// <summary>
    /// Subscription requests call the selector TargetEventStore, not EventStore.
    /// </summary>
    static readonly IReadOnlySet<string> _explicitSelectors = new HashSet<string>(StringComparer.Ordinal)
    {
        "Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions.IEventStoreSubscriptions.Add",
        "Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions.IEventStoreSubscriptions.Remove",
        "Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions.IEventStoreSubscriptions.GetSubscriptions"
    };

    public static IEnumerable<string> For(Type contract) =>
        contract.GetMethods()
            .Where(method => _explicitSelectors.Contains(Identity(method)) ||
                method.GetParameters().Any(parameter => HasStoreSelector(parameter.ParameterType, contract.Assembly, [])))
            .Select(Identity);

    public static string Identity(MethodInfo method) => $"{method.DeclaringType!.FullName}.{method.Name}";

    public static IEnumerable<string> Unlisted(IEnumerable<string> discovered, IReadOnlyDictionary<string, store_scoped_surface> inventory) =>
        discovered.Where(id => !inventory.ContainsKey(id));

    static bool HasStoreSelector(Type type, Assembly contracts, HashSet<Type> visited)
    {
        if (!visited.Add(type))
        {
            return false;
        }

        if (type.IsArray)
        {
            return HasStoreSelector(type.GetElementType()!, contracts, visited);
        }

        // Unwrap collections, observable duplex messages and OneOf payloads before inspecting nested requests.
        if (type.IsGenericType && type.GetGenericArguments().Any(argument => HasStoreSelector(argument, contracts, visited)))
        {
            return true;
        }

        return type.Assembly == contracts && type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Any(property => IsStoreSelector(property) || HasStoreSelector(property.PropertyType, contracts, visited));
    }

    static bool IsStoreSelector(PropertyInfo property) =>
        property.Name == "EventStore" && (property.PropertyType == typeof(string) || property.PropertyType == typeof(EventStoreName));
}
