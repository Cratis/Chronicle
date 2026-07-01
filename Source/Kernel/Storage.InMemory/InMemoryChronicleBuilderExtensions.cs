// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.InMemory;
using Cratis.Chronicle.Storage.InMemory.Compliance;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Extension methods for <see cref="IChronicleBuilder"/> for configuring Chronicle to use in-memory storage.
/// </summary>
/// <remarks>
/// The in-memory backend keeps all event sequences, observers, read-model sinks, and system state in process
/// memory. It is ideal for tests, samples, and ephemeral environments where durability is not required — all
/// state is lost when the process exits.
/// </remarks>
public static class InMemoryChronicleBuilderExtensions
{
    /// <summary>
    /// Configure Chronicle to use in-memory storage, based on the <see cref="ChronicleOptions"/>.
    /// </summary>
    /// <param name="builder"><see cref="IChronicleBuilder"/> to configure.</param>
    /// <param name="options"><see cref="ChronicleOptions"/> to use.</param>
    /// <returns><see cref="IChronicleBuilder"/> for continuation.</returns>
    public static IChronicleBuilder WithInMemory(this IChronicleBuilder builder, ChronicleOptions options)
    {
        _ = options;
        return builder.WithInMemory();
    }

    /// <summary>
    /// Configure Chronicle to use in-memory storage.
    /// </summary>
    /// <param name="builder"><see cref="IChronicleBuilder"/> to configure.</param>
    /// <returns><see cref="IChronicleBuilder"/> for continuation.</returns>
    public static IChronicleBuilder WithInMemory(this IChronicleBuilder builder)
    {
        builder.SiloBuilder.UseInMemoryReminderService();

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<EventStoreStorages>();
            services.AddSingleton<ISystemStorage, SystemStorage>();
            services.AddSingleton<IClusterStorage, ClusterStorage>();
            services.AddSingleton<IEncryptionKeyStorage, EncryptionKeyStorage>();
            services.AddSingleton<IStorage, Cratis.Chronicle.Storage.InMemory.Storage>();
        });

        return builder;
    }
}
