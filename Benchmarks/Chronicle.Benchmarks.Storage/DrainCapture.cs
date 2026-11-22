// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Benchmarks.Storage.Hosts;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>
/// Attributes one untimed full drain on a fresh sequence, outside benchmark measurements.
/// </summary>
internal static class DrainCapture
{
    /// <summary>
    /// Appends one fixture size, keeping diagnostic errors non-fatal.
    /// </summary>
    /// <param name="host">The provider host.</param>
    /// <param name="size">The fresh fixture event count.</param>
    /// <param name="directory">The provider artifact directory.</param>
    /// <returns>Completion for the harness budget race.</returns>
    public static async Task<bool> Run(IStorageProviderHost host, int size, string directory)
    {
        var path = Path.Combine(directory, "drain-pages.md");
        if (!File.Exists(path))
        {
            await File.WriteAllTextAsync(path, "# Drain page attribution\n\nMoveNext includes storage reads, materialization and conversion. DB-read-only time is not isolated: command execution interceptors exclude reader consumption. Zero-event rows are terminal probes and excluded from page/quarter statistics.\n\n");
        }

        try
        {
            RunBudget.Current.ThrowIfExpired();
            var storage = host.CreateSequence();
            await StorageBenchmark.Seed(storage, size);
            var started = Stopwatch.GetTimestamp();
            using var cursor = await storage.GetFromSequenceNumber(0, cancellationToken: RunBudget.Current.Token);
            var pages = await Read(cursor);
            var total = Stopwatch.GetElapsedTime(started);
            await File.AppendAllTextAsync(path, DrainReport.Format(size, pages, total));
        }
        catch (Exception error)
        {
            await File.AppendAllTextAsync(path, $"## {size} events\n\nnot captured: {error.Message}\n\n");
        }

        return true;
    }

    static async Task<IReadOnlyList<DrainPage>> Read(IEventCursor cursor)
    {
        var pages = new List<DrainPage>();
        ulong next = 0;
        while (true)
        {
            RunBudget.Current.ThrowIfExpired();
            var started = Stopwatch.GetTimestamp();
            var moved = await cursor.MoveNext();
            var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            var events = moved ? cursor.Current.ToArray() : [];
            pages.Add(new(pages.Count, next, events.Length, elapsed));
            if (!moved) return pages;
            next = events.Length == 0 ? next : (ulong)events[^1].Context.SequenceNumber + 1;
        }
    }
}
