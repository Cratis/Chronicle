// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Extension methods for <see cref="IChronicleBuilder"/> for registering a dedicated compliance
/// <see cref="IEncryptionKeyStorage"/>.
/// </summary>
public static class ComplianceEncryptionKeyStorageExtensions
{
    /// <summary>
    /// Register a dedicated <see cref="IEncryptionKeyStorage"/> for compliance, replacing the one the general
    /// storage backend registered.
    /// </summary>
    /// <remarks>
    /// When <see cref="Encryption.MigrateFromDefaultStorage"/> is set, the two are composed instead of replaced:
    /// the dedicated store becomes the primary of a <see cref="CompositeEncryptionKeyStorage"/> and the storage
    /// the backend already registered becomes its secondary, so keys that only exist in the default storage keep
    /// being served and are written into the dedicated store as they are read.
    /// </remarks>
    /// <param name="builder"><see cref="IChronicleBuilder"/> to configure.</param>
    /// <param name="options"><see cref="ChronicleOptions"/> to use.</param>
    /// <param name="dedicatedStorage">Factory for the dedicated <see cref="IEncryptionKeyStorage"/>.</param>
    /// <returns><see cref="IChronicleBuilder"/> for continuation.</returns>
    public static IChronicleBuilder WithComplianceEncryptionKeyStorage(
        this IChronicleBuilder builder,
        ChronicleOptions options,
        Func<IServiceProvider, IEncryptionKeyStorage> dedicatedStorage)
    {
        var defaultStorage = options.Compliance.Encryption.MigrateFromDefaultStorage
            ? builder.Services.LastOrDefault(_ => _.ServiceType == typeof(IEncryptionKeyStorage) && !_.IsKeyedService)
            : null;

        if (defaultStorage is null)
        {
            builder.Services.AddSingleton(dedicatedStorage);
            return builder;
        }

        // The backend's registration is taken over rather than merely shadowed: leaving it in place would build a
        // second instance of the same store, and a second cache in front of it that nothing ever evicts.
        builder.Services.Remove(defaultStorage);
        builder.Services.AddSingleton<IEncryptionKeyStorage>(sp => new CompositeEncryptionKeyStorage(
            sp.GetRequiredService<ILogger<CompositeEncryptionKeyStorage>>(),
            dedicatedStorage(sp),
            Materialize(defaultStorage, sp)));

        return builder;
    }

    /// <summary>Register a dedicated store against the selected default backend's captured fence authority.</summary>
    /// <param name="builder">The Chronicle builder.</param>
    /// <param name="options">The Chronicle options.</param>
    /// <param name="dedicatedStorage">Creates the store with its authority, independent of migration settings.</param>
    /// <returns>The builder.</returns>
    /// <exception cref="InvalidOperationException">The default backend did not register an authority.</exception>
    public static IChronicleBuilder WithComplianceEncryptionKeyStorage(
        this IChronicleBuilder builder,
        ChronicleOptions options,
        Func<IServiceProvider, IEncryptionKeyFence, IEncryptionKeyStorage> dedicatedStorage)
    {
        var descriptor = builder.Services.LastOrDefault(_ => _.ServiceType == typeof(IEncryptionKeyFence) && !_.IsKeyedService) ??
            throw new InvalidOperationException("A default encryption key fence authority must be registered before dedicated compliance key storage.");
        return builder.WithComplianceEncryptionKeyStorage(options, serviceProvider => dedicatedStorage(serviceProvider, MaterializeFence(descriptor, serviceProvider)));
    }

    static IEncryptionKeyFence MaterializeFence(ServiceDescriptor descriptor, IServiceProvider serviceProvider) =>
        descriptor switch
        {
            { ImplementationInstance: IEncryptionKeyFence instance } => instance,
            { ImplementationFactory: { } factory } => (IEncryptionKeyFence)factory(serviceProvider),
            { ImplementationType: { } type } => (IEncryptionKeyFence)ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, type),
            _ => throw new UnresolvableDefaultEncryptionKeyStorage()
        };

    static IEncryptionKeyStorage Materialize(ServiceDescriptor descriptor, IServiceProvider serviceProvider) =>
        descriptor switch
        {
            { ImplementationInstance: IEncryptionKeyStorage instance } => instance,
            { ImplementationFactory: { } factory } => (IEncryptionKeyStorage)factory(serviceProvider),
            { ImplementationType: { } type } => (IEncryptionKeyStorage)ActivatorUtilities.CreateInstance(serviceProvider, type),
            _ => throw new UnresolvableDefaultEncryptionKeyStorage()
        };
}
