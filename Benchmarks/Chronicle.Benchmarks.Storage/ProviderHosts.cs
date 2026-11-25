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

    /// <summary>Disposes started hosts within a single 60-second shutdown grace period.</summary>
    /// <returns>The bounded teardown task.</returns>
    public static Task DisposeAll()
    {
        var started = Started();
        _hosts.Clear();
        return DisposeAll(started, TimeSpan.FromSeconds(60));
    }

    /// <summary>Bounds disposal without waiting for unfinished startup tasks.</summary>
    /// <param name="started">The startup tasks to tear down.</param>
    /// <param name="grace">The total shutdown grace period.</param>
    /// <returns>The bounded teardown task.</returns>
    internal static async Task DisposeAll(IEnumerable<Task<IStorageProviderHost>> started, TimeSpan grace)
    {
        var disposal = Task.WhenAll(started.Select(DisposeOrDefer));
        try
        {
            await disposal.WaitAsync(grace);
        }
        catch (TimeoutException)
        {
            await Console.Error.WriteLineAsync("Host disposal skipped: shutdown grace period exhausted.");
        }
    }

    static Task DisposeOrDefer(Task<IStorageProviderHost> startup)
    {
        if (startup.IsCompleted) return DisposeCompleted(startup);

        // A late host is still cleaned up, but its startup cannot hold the harness exit open.
        _ = startup.ContinueWith(DisposeCompleted, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();
        return Task.CompletedTask;
    }

    static async Task DisposeCompleted(Task<IStorageProviderHost> started)
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

    static async Task<IStorageProviderHost> StartAsync(StorageProvider provider)
    {
        IStorageProviderHost host = provider == StorageProvider.Mongo ? new MongoProviderHost() : new SqlProviderHost(provider);
        return await StartGuard.StartOrDispose(host, started => started.Start());
    }
}
