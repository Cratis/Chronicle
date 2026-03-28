// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator;

/// <summary>
/// Discovers commands and queries from a loaded assembly.
/// </summary>
/// <param name="assembly">The assembly to discover types from.</param>
public class TypeDiscovery(Assembly assembly)
{
    const string CommandAttributeName = "CommandAttribute";
    const string ReadModelAttributeName = "ReadModelAttribute";
    const string BelongsToAttributeName = "BelongsToAttribute";

    /// <summary>
    /// Discovers all service definitions from the assembly.
    /// </summary>
    /// <returns>A dictionary keyed by "namespace.ServiceName" containing service definitions.</returns>
    /// <exception cref="NamespaceMismatchException">Thrown when types for the same service are in different namespaces.</exception>
    public IDictionary<string, ServiceDefinition> DiscoverServices()
    {
        var services = new Dictionary<string, ServiceDefinition>();

        foreach (var type in assembly.GetTypes())
        {
            if (!type.IsClass || type.IsAbstract)
            {
                continue;
            }

            var belongsTo = GetBelongsToAttribute(type);
            if (belongsTo is null)
            {
                if (HasCommandAttribute(type) || HasReadModelAttribute(type))
                {
                    Console.WriteLine($"  WARNING: Type '{type.FullName}' has [Command] or [ReadModel] but no [BelongsTo] attribute. Skipping.");
                }

                continue;
            }

            var serviceName = belongsTo;
            var typeNamespace = type.Namespace ?? string.Empty;
            var key = $"{typeNamespace}.{serviceName}";

            if (!services.TryGetValue(key, out var serviceDefinition))
            {
                serviceDefinition = new ServiceDefinition(serviceName, typeNamespace);
                services[key] = serviceDefinition;
            }

            if (serviceDefinition.Namespace != typeNamespace)
            {
                throw new NamespaceMismatchException(
                    serviceName,
                    serviceDefinition.Namespace,
                    typeNamespace,
                    type.FullName ?? type.Name);
            }

            if (HasCommandAttribute(type))
            {
                serviceDefinition.Commands.Add(new CommandDefinition(type));
            }
            else if (HasReadModelAttribute(type))
            {
                var queryMethods = DiscoverQueryMethods(type);
                if (queryMethods.Count > 0)
                {
                    serviceDefinition.Queries.Add(new QueryDefinition(type, queryMethods));
                }
            }
        }

        return services;
    }

    static bool HasCommandAttribute(Type type) =>
        type.GetCustomAttributesData().Any(a => a.AttributeType.Name == CommandAttributeName);

    static bool HasReadModelAttribute(Type type) =>
        type.GetCustomAttributesData().Any(a => a.AttributeType.Name == ReadModelAttributeName);

    static string? GetBelongsToAttribute(Type type)
    {
        var attr = type.GetCustomAttributesData()
            .FirstOrDefault(a => a.AttributeType.Name == BelongsToAttributeName);

        if (attr is null)
        {
            return null;
        }

        return attr.ConstructorArguments.FirstOrDefault().Value as string;
    }

    static List<QueryMethodDefinition> DiscoverQueryMethods(Type readModelType)
    {
        var methods = new List<QueryMethodDefinition>();

        foreach (var method in readModelType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
        {
            if (method.DeclaringType == typeof(object))
            {
                continue;
            }

            if (method.IsSpecialName)
            {
                continue;
            }

            // Skip private static helpers — only public and internal (assembly) static methods define query contracts
            if (method.IsPrivate)
            {
                continue;
            }

            methods.Add(new QueryMethodDefinition(method));
        }

        return methods;
    }
}
