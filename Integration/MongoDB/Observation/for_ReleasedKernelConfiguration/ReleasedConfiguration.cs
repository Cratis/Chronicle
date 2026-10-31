// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.MongoDB.Integration.Observation.for_ReleasedKernelConfiguration;

static class ReleasedConfiguration
{
    internal const string OriginalStorageTypeKey = "Chronicle__Storage__Clusters__Default__Type";
    internal const string OriginalStorageConnectionKey = "Chronicle__Storage__Clusters__Default__ConnectionDetails";
    const string TypeSentinel = "ReleasedBindingSentinel";
    const string ConnectionSentinel = "mongodb://released-binding.invalid:27017";

    static readonly string[] _storageKeys =
    [
        "Cratis__Chronicle__Storage__Type",
        "Cratis__Chronicle__Storage__ConnectionDetails",
        OriginalStorageTypeKey,
        OriginalStorageConnectionKey
    ];

    internal static (bool TypeMatches, bool ConnectionMatches, bool OptionsAreReleased, bool StorageIsReleased) Bind(string typeKey, string connectionKey)
    {
        var previous = _storageKeys.Append(typeKey).Append(connectionKey).Distinct().ToDictionary(key => key, Environment.GetEnvironmentVariable);
        try
        {
            foreach (var key in previous.Keys) Environment.SetEnvironmentVariable(key, null);
            Environment.SetEnvironmentVariable(typeKey, TypeSentinel);
            Environment.SetEnvironmentVariable(connectionKey, ConnectionSentinel);
            return BindReleasedOptions();
        }
        finally
        {
            foreach (var (key, value) in previous) Environment.SetEnvironmentVariable(key, value);
        }
    }

    static (bool TypeMatches, bool ConnectionMatches, bool OptionsAreReleased, bool StorageIsReleased) BindReleasedOptions()
    {
        var directory = Environment.GetEnvironmentVariable("CHRONICLE_RELEASED_RUNTIME_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("CHRONICLE_RELEASED_RUNTIME_DIRECTORY must name the CI-extracted pinned released runtime; HEAD options are not a substitute.");
        }

        var assemblyPath = Path.GetFullPath(Path.Combine(directory, "Cratis.Chronicle.Core.dll"));
        if (assemblyPath == typeof(ChronicleOptions).Assembly.Location)
        {
            throw new InvalidOperationException("The released configuration assembly must not be the current Core assembly.");
        }

        var context = new ReleasedConfigurationLoadContext(Path.GetDirectoryName(assemblyPath)!);
        try
        {
            var assembly = context.LoadFromAssemblyPath(assemblyPath);
            return BindStorage(assembly, assemblyPath, context);
        }
        finally
        {
            context.Unload();
        }
    }

    static (bool TypeMatches, bool ConnectionMatches, bool OptionsAreReleased, bool StorageIsReleased) BindStorage(Assembly assembly, string assemblyPath, AssemblyLoadContext context)
    {
        var optionsType = assembly.GetType("Cratis.Chronicle.Configuration.ChronicleOptions", throwOnError: true)!;
        using var configuration = new ConfigurationManager();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        var addConfiguration = optionsType.GetMethod("AddConfiguration", [typeof(IServiceCollection), typeof(IConfiguration)])!;
        addConfiguration.Invoke(null, [services, configuration]);
        using var provider = services.BuildServiceProvider();
        var optionsContract = typeof(IOptions<>).MakeGenericType(optionsType);
        var options = optionsContract.GetProperty("Value")!.GetValue(provider.GetRequiredService(optionsContract))!;
        var storage = optionsType.GetProperty("Storage")!.GetValue(options)!;
        var storageType = storage.GetType();
        return (
            Equals(storageType.GetProperty("Type")!.GetValue(storage), TypeSentinel),
            Equals(storageType.GetProperty("ConnectionDetails")!.GetValue(storage), ConnectionSentinel),
            IsReleased(options.GetType(), assembly, assemblyPath, context),
            IsReleased(storageType, assembly, assemblyPath, context));
    }

    static bool IsReleased(Type type, Assembly assembly, string assemblyPath, AssemblyLoadContext context) =>
        type.Assembly == assembly &&
        type.Assembly != typeof(ChronicleOptions).Assembly &&
        type.Assembly.Location == assemblyPath &&
        AssemblyLoadContext.GetLoadContext(type.Assembly) == context;
}
