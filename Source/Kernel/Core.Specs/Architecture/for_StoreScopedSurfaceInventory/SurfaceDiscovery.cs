// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Queries.ModelBound;
using Microsoft.AspNetCore.Mvc;
using ProtoBuf.Grpc.Configuration;

namespace Cratis.Chronicle.Architecture.for_StoreScopedSurfaceInventory;

public static class SurfaceDiscovery
{
    const BindingFlags DeclaredMethods = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    public static IEnumerable<KeyValuePair<string, MethodInfo>> Discover()
    {
        // Do not filter on [Operation]: code-first services also expose methods without it.
        var contracts = typeof(Contracts.Host.IServer).Assembly.GetTypes()
            .Where(type => type.IsInterface && type.IsDefined(typeof(ServiceAttribute)));
        var grpc = contracts.SelectMany(type => type.GetInterfaces().Append(type)
            .SelectMany(contract => contract.GetMethods())
            .Distinct()
            .Select(method => Entry("gRPC", type, method, "Cratis.Chronicle.Contracts.")));

        // No [BelongsTo] filter: Arc exposes artifacts that have no generated gRPC equivalent too.
        var arc = typeof(EventStores.EnsureEventStore).Assembly.GetTypes()
            .SelectMany(type => type.GetMethods(DeclaredMethods)
                .Where(method => IsArcEntryPoint(type, method))
                .Select(method => Entry("Arc", type, method, "Cratis.Chronicle.")));
        return grpc.Concat(arc);
    }

    public static bool HasEventStoreArgument(MethodInfo method) =>
        (method.DeclaringType!.IsDefined(typeof(CommandAttribute)) && method.DeclaringType.GetProperty("EventStore") is not null) ||
        method.GetParameters().Any(parameter =>
            string.Equals(parameter.Name, "eventStore", StringComparison.OrdinalIgnoreCase) ||
            parameter.ParameterType.GetProperty("EventStore") is not null);

    static KeyValuePair<string, MethodInfo> Entry(string transport, Type type, MethodInfo method, string prefix) =>
        new($"{transport}:{type.FullName![prefix.Length..]}.{method.Name}", method);

    static bool IsArcEntryPoint(Type type, MethodInfo method) =>
        IsCommandHandler(type, method) || IsModelQuery(type, method) ||
        method.GetCustomAttributesData().Any(attribute => attribute.AttributeType.Name == "QueryAttribute") ||
        IsControllerAction(type, method);

    static bool IsCommandHandler(Type type, MethodInfo method) =>
        type.IsDefined(typeof(CommandAttribute)) && method.Name == "Handle" && !method.IsStatic;

    /// <summary>
    /// Mirrors the code-first generator's query discovery, including internal static queries.
    /// </summary>
    /// <param name="type">The declaring read model type.</param>
    /// <param name="method">The candidate query method.</param>
    static bool IsModelQuery(Type type, MethodInfo method) =>
        type.IsDefined(typeof(ReadModelAttribute)) && method.IsStatic && !method.IsPrivate && !method.IsSpecialName;

    static bool IsControllerAction(Type type, MethodInfo method) =>
        typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract &&
        method.IsPublic && !method.IsStatic && !method.IsSpecialName && !method.IsDefined(typeof(NonActionAttribute));
}
