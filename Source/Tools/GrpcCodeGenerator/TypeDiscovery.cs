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

        IEnumerable<Type> types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Some assemblies (e.g. Orleans grain assemblies) have types whose base-class
            // chains cannot be fully loaded in the isolated context.  Use only the types
            // that loaded successfully and emit a warning for each failure.
            foreach (var loaderEx in ex.LoaderExceptions.OfType<Exception>())
            {
                Console.WriteLine($"  WARNING: Failed to load one or more types: {loaderEx.Message}");
            }

            types = ex.Types.OfType<Type>();
        }

        foreach (var type in types)
        {
            bool isClass;
            bool isAbstract;
            try
            {
                isClass = type.IsClass;
                isAbstract = type.IsAbstract;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  WARNING: Skipping type '{type.FullName}' — could not determine class/abstract status: {ex.Message}");
                continue;
            }

            if (!isClass || isAbstract)
            {
                continue;
            }

            var belongsTo = GetBelongsToAttribute(type);
            if (belongsTo is null)
            {
                bool hasCommandAttr;
                bool hasReadModelAttr;
                try
                {
                    hasCommandAttr = HasCommandAttribute(type);
                    hasReadModelAttr = HasReadModelAttribute(type);
                }
                catch
                {
                    hasCommandAttr = false;
                    hasReadModelAttr = false;
                }

                if (hasCommandAttr || hasReadModelAttr)
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

            bool isCommand;
            bool isReadModel;
            try
            {
                isCommand = HasCommandAttribute(type);
                isReadModel = HasReadModelAttribute(type);
            }
            catch
            {
                continue;
            }

            if (isCommand)
            {
                serviceDefinition.Commands.Add(new CommandDefinition(type));
            }
            else if (isReadModel)
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

    static bool HasCommandAttribute(Type type)
    {
        try
        {
            return type.GetCustomAttributesData().Any(a => a.AttributeType.Name == CommandAttributeName);
        }
        catch (Exception)
        {
            return false;
        }
    }

    static bool HasReadModelAttribute(Type type)
    {
        try
        {
            return type.GetCustomAttributesData().Any(a => a.AttributeType.Name == ReadModelAttributeName);
        }
        catch (Exception)
        {
            return false;
        }
    }

    static string? GetBelongsToAttribute(Type type)
    {
        IList<CustomAttributeData> attrs;
        try
        {
            attrs = type.GetCustomAttributesData();
        }
        catch
        {
            return null;
        }

        var attr = attrs.FirstOrDefault(a =>
        {
            try { return a.AttributeType.Name == BelongsToAttributeName; }
            catch { return false; }
        });

            if (attr is null)
            {
                return null;
            }

            return attr.ConstructorArguments.FirstOrDefault().Value as string;
        }
        catch (Exception)
        {
            return null;
        }
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
