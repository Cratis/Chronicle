// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using Cratis.Chronicle.Benchmarks.Storage.Hosts;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>
/// Base for the storage benchmarks: gives access to the running provider host and measures round trips around the
/// work BenchmarkDotNet times.
/// </summary>
public abstract class StorageBenchmark
{
    /// <summary>Gets the providers of this run, for `ParamsSource`.</summary>
    public static IEnumerable<StorageProvider> Providers => StorageRunOptions.Current.Providers;

    /// <summary>Gets the profile of this run.</summary>
    public static BenchmarkProfile Profile => StorageRunOptions.Current.Profile;

    /// <summary>Gets or sets the provider the case runs against.</summary>
    [ParamsSource(nameof(Providers))]
    public StorageProvider Provider { get; set; }

    /// <summary>Gets the started host.</summary>
    internal IStorageProviderHost Host { get; private set; } = null!;

    /// <summary>Reads every event a cursor yields.</summary>
    /// <param name="cursor">The cursor.</param>
    /// <returns>The number of events read.</returns>
    protected static async Task<int> Drain(IEventCursor cursor)
    {
        using (cursor)
        {
            var count = 0;
            while (await cursor.MoveNext()) count += cursor.Current.Count();
            return count;
        }
    }

    /// <summary>Seeds a sequence with events in batches, outside measurement.</summary>
    /// <param name="storage">The storage.</param>
    /// <param name="count">How many events.</param>
    /// <returns>The task.</returns>
    /// <exception cref="InvalidOperationException">A batch append failed.</exception>
    protected static async Task Seed(IEventSequenceStorage storage, int count)
    {
        const int batch = 1000;
        for (ulong first = 0; first < (ulong)count; first += batch)
        {
            var size = (int)Math.Min(batch, (ulong)count - first);
            var result = await storage.AppendMany(Enumerable.Range(0, size).Select(offset => BenchmarkEvents.ToAppend(first + (ulong)offset, Profile.PayloadBytes)));
            if (!result.IsSuccess) throw new InvalidOperationException($"Seeding failed at {first}.");
        }
    }

    /// <summary>Starts (or reuses) the provider host.</summary>
    /// <returns>The task.</returns>
    protected async Task StartHost() => Host = await ProviderHosts.Get(Provider);

    /// <summary>Builds the key a case records its counts under.</summary>
    /// <param name="scenario">The scenario name.</param>
    /// <param name="parameter">The case parameter, if any.</param>
    /// <returns>The key.</returns>
    protected string Key(string scenario, object? parameter = null) =>
        $"{StorageProviderNames.NameOf(Provider)}/{scenario}{(parameter is null ? string.Empty : $"/{parameter}")}";

    /// <summary>Runs one operation and records its round trips and latency.</summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="key">The case key.</param>
    /// <param name="events">A function giving the number of events the result carries or wrote.</param>
    /// <param name="operation">The operation.</param>
    /// <returns>The operation's result.</returns>
    protected async Task<T> Measure<T>(string key, Func<T, long> events, Func<Task<T>> operation)
    {
        var before = Host.Counter.Snapshot();
        var started = Stopwatch.GetTimestamp();
        var stats = Measurements.For(key);
        try
        {
            var result = await operation();
            stats.Record(events(result), Host.Counter.Snapshot().Since(before), Stopwatch.GetElapsedTime(started));
            return result;
        }
        catch (Exception)
        {
            stats.RecordError();
            throw;
        }
    }
}
