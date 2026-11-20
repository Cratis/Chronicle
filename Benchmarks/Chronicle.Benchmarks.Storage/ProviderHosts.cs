// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Benchmarks.Storage.Hosts;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>
/// Starts each requested provider once per process and hands the same running host to every benchmark case, so
/// containers are not restarted per case. <see cref="Program"/> disposes them at the end of the run.
/// </summary>
internal static class ProviderHosts
{
    static readonly ConcurrentDictionary<StorageProvider, Lazy<Task<IStorageProviderHost>>> _hosts = new();

    /// <summary>Gets the started host for a provider.</summary>
    /// <param name="provider">The provider.</param>
    /// <returns>The host.</returns>
    public static Task<IStorageProviderHost> Get(StorageProvider provider) =>
        _hosts.GetOrAdd(provider, static key => new(() => StartAsync(key))).Value;

    /// <summary>Gets the providers that have been started.</summary>
    /// <returns>The hosts.</returns>
    public static IReadOnlyCollection<Task<IStorageProviderHost>> Started() => [.. _hosts.Values.Where(_ => _.IsValueCreated).Select(_ => _.Value)];

    /// <summary>Disposes every started host.</summary>
    /// <returns>The task.</returns>
    public static async Task DisposeAll()
    {
        foreach (var started in Started())
        {
            try
            {
                await (await started).DisposeAsync();
            }
            catch (Exception error)
            {
                // A host that never started has nothing to dispose.
                await Console.Error.WriteLineAsync($"Host disposal skipped: {error.Message}");
            }
        }

        _hosts.Clear();
    }

    static async Task<IStorageProviderHost> StartAsync(StorageProvider provider)
    {
        IStorageProviderHost host = provider == StorageProvider.Mongo ? new MongoProviderHost() : new SqlProviderHost(provider);
        await host.Start();
        return host;
    }
}
