// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Reflection;
using Aksio.Cratis.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aksio.Cratis.Configuration;

/// <summary>
/// Represents a system that is able to discover and configure all configuration objects based on <see cref="ConfigurationAttribute"/>.
/// </summary>
public static class ConfigurationObjects
{
    record ConfigurationObjectPerTenantKey(Type ConfigurationObjectType, TenantId TenantId);

    static readonly ConcurrentDictionary<ConfigurationObjectPerTenantKey, object> _configurationObjectsPerTenant = new();

    /// <summary>
    /// Discover all configuration object types based on <see cref="ConfigurationAttribute"/> and add them to the IoC container.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <param name="types"><see cref="ITypes"/> for discovery.</param>
    /// <param name="searchSubPaths">All search sub paths.</param>
    /// <param name="logger">Optional logger.</param>
    public static void DiscoverAndAddAllConfigurationObjects(IServiceCollection services, ITypes types, IEnumerable<string> searchSubPaths, ILogger? logger = default)
    {
        foreach (var configurationObjectType in types.All.Where(_ => _.HasAttribute<ConfigurationAttribute>()))
        {
            var attribute = configurationObjectType.GetCustomAttribute<ConfigurationAttribute>()!;
            var optionsType = typeof(IOptions<>).MakeGenericType(configurationObjectType);
            var optionsWrapperType = typeof(OptionsWrapper<>).MakeGenericType(configurationObjectType);

            if (attribute.PerTenant)
            {
                services.AddTransient(configurationObjectType, _ =>
                    TryResolveConfigurationObjectForCurrentTenant(
                        attribute,
                        configurationObjectType,
                        searchSubPaths,
                        logger));

                services.AddTransient(optionsType, _ =>
                {
                    var configurationObject = TryResolveConfigurationObjectForCurrentTenant(
                        attribute,
                        configurationObjectType,
                        searchSubPaths,
                        logger);
                    return Activator.CreateInstance(optionsWrapperType, new[] { configurationObject })!;
                });
            }
            else
            {
                var configurationObject = TryBindConfiguration(
                    attribute,
                    configurationObjectType,
                    searchSubPaths,
                    logger,
                    configurationName => Path.HasExtension(configurationName) ? configurationName : $"{configurationName}.json",
                    out _);
                if (configurationObject is not null)
                {
                    services.AddChildConfigurationObjects(configurationObjectType, configurationObject);

                    var optionsWrapperInstance = Activator.CreateInstance(optionsWrapperType, new[] { configurationObject });
                    services.AddSingleton(configurationObjectType, configurationObject);
                    services.AddSingleton(optionsType, optionsWrapperInstance!);
                }
            }
        }
    }

    static object TryResolveConfigurationObjectForCurrentTenant(ConfigurationAttribute attribute, Type configurationObjectType, IEnumerable<string> searchSubPaths, ILogger? logger)
    {
        var tenantId = ExecutionContextManager.GetCurrent().TenantId;
        var key = new ConfigurationObjectPerTenantKey(configurationObjectType, tenantId);
        if (_configurationObjectsPerTenant.ContainsKey(key))
        {
            return _configurationObjectsPerTenant[key];
        }

        var configurationObject = TryBindConfiguration(
            attribute,
            configurationObjectType,
            searchSubPaths,
            logger,
            configurationName => Path.Combine(tenantId.ToString(), Path.HasExtension(configurationName) ? configurationName : $"{configurationName}.json"),
            out var configurationFileName);
        if (configurationObject is not null)
        {
            _configurationObjectsPerTenant[key] = configurationObject;
            return configurationObject;
        }

        throw new MissingConfigurationObjectForTenant(configurationObjectType, tenantId, configurationFileName);
    }

    static object? TryBindConfiguration(
        ConfigurationAttribute attribute,
        Type configurationObjectType,
        IEnumerable<string> searchSubPaths,
        ILogger? logger,
        Func<string, string> getConfigurationFileName,
        out string configurationFileName)
    {
        var configurationName = attribute.NameSet ? attribute.Name : configurationObjectType.Name.ToLowerInvariant();
        configurationFileName = getConfigurationFileName(configurationName);

        var configurationBuilder = new ConfigurationBuilder();

        foreach (var searchPath in searchSubPaths)
        {
            logger?.AddingConfigurationFile(configurationObjectType, configurationName, searchPath);
            var actualFile = Path.Combine(searchPath, configurationFileName);
            configurationBuilder.AddJsonFile(actualFile, true);
        }

        var configuration = configurationBuilder.Build();
        var configurationObject = Activator.CreateInstance(configurationObjectType)!;
        ResolveConfigurationValues(configuration, configurationObjectType, configurationObject);

        if (configuration.Providers.Any(_ => _.GetChildKeys(Array.Empty<string>(), null!).Any()))
        {
            configuration.Bind(configurationObject);

            if (configurationObject is IPerformPostBindOperations postPerformer)
            {
                postPerformer.Perform();
            }

            return configurationObject;
        }

        return null!;
    }

    static void ResolveConfigurationValues(IConfiguration configuration, Type configurationObjectType, object configurationObject)
    {
        foreach (var property in configurationObjectType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(_ => _.CanWrite))
        {
            object? propertyValue = null;

            if (property.HasAttribute<ConfigurationValueResolverAttribute>())
            {
                var resolverAttribute = property.GetCustomAttribute<ConfigurationValueResolverAttribute>()!;
                var resolver = (Activator.CreateInstance(resolverAttribute.ResolverType) as IConfigurationValueResolver)!;
                propertyValue = resolver.Resolve(configuration);
                property.SetValue(configurationObject, propertyValue);
            }
            else if (property.GetIndexParameters().Length == 0)
            {
                propertyValue = property.GetValue(configurationObject)!;
            }

            if (propertyValue is not null &&
                !property.PropertyType.IsAPrimitiveType() &&
                !property.PropertyType.IsEnumerable())
            {
                ResolveConfigurationValues(configuration.GetSection(property.Name), property.PropertyType, propertyValue);
            }
        }
    }

    static void AddChildConfigurationObjects(this IServiceCollection services, Type configurationObjectType, object configurationObject)
    {
        foreach (var childProperty in configurationObjectType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(_ => _.PropertyType.HasAttribute<ConfigurationAttribute>()))
        {
            var childConfigurationObject = childProperty.GetValue(configurationObject)!;
            services.AddSingleton(childProperty.PropertyType, childConfigurationObject);
            services.AddChildConfigurationObjects(childProperty.PropertyType, childConfigurationObject);
        }
    }
}
