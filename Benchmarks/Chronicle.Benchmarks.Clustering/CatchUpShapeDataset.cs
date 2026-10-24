// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Generates and verifies the interleaved catch-up backlog.
/// </summary>
/// <param name="settings">The benchmark settings.</param>
internal sealed class CatchUpShapeDataset(CatchUpShapeSettings settings)
{
    readonly Dictionary<string, int> _expected = new(StringComparer.Ordinal) { [Source(0)] = 0 };

    /// <summary>
    /// Gets the source identifier for an index.
    /// </summary>
    /// <param name="index">The source index.</param>
    /// <returns>The source identifier.</returns>
    public static string Source(int index) => $"catchup-source-{index.ToString("D8", CultureInfo.InvariantCulture)}";

    /// <summary>
    /// Appends the backlog of events.
    /// </summary>
    /// <param name="fixture">The cluster fixture.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a seed batch is not acknowledged successfully.</exception>
    public async Task SeedBacklog(ClusterBenchmarkFixture fixture)
    {
        var batch = new List<EventForEventSourceId>(settings.BatchSize);
        for (var index = 0; index < settings.Events; index++)
        {
            var source = Source(index % settings.Sources);
            var value = index + 1;
            _expected[source] = value;
            batch.Add(new(source, new CatchUpShapeValueSet(value)));
            if (batch.Count == settings.BatchSize)
            {
                if (!(await fixture.EventStore1.EventLog.AppendMany(batch)).IsSuccess)
                {
                    throw new InvalidOperationException("A catch-up seed batch was not acknowledged successfully.");
                }
                batch.Clear();
            }
        }

        if (batch.Count > 0 && !(await fixture.EventStore1.EventLog.AppendMany(batch)).IsSuccess)
        {
            throw new InvalidOperationException("A catch-up seed batch was not acknowledged successfully.");
        }
    }

    /// <summary>
    /// Verifies the materialized read models.
    /// </summary>
    /// <param name="fixture">The cluster fixture.</param>
    /// <returns>A fingerprint of the snapshot, or null for the reactor.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the snapshot does not match the expected state.</exception>
    public async Task<string?> Verify(ClusterBenchmarkFixture fixture)
    {
        // The reactor intentionally does no work; its durable observer position and completed job are the completion proof.
        if (settings.ObserverType == "reactor") return null;
        var actual = (await fixture.EventStore1.ReadModels.Materialized.GetInstances<CatchUpShapeReadModel>(take: _expected.Count + 1)).ToDictionary(model => model.Id, model => model.Value, StringComparer.Ordinal);
        if (actual.Count != _expected.Count || _expected.Any(pair => !actual.TryGetValue(pair.Key, out var value) || value != pair.Value))
        {
            throw new InvalidOperationException($"Catch-up snapshot mismatch: expected {_expected.Count} sources, got {actual.Count}.");
        }

        var text = string.Join('\n', actual.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key}:{pair.Value.ToString(CultureInfo.InvariantCulture)}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }
}
