// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

public partial class ReindexConstraintsStep
{
    async Task<bool> ReportCollisions(RebuildProgress progress, CancellationToken cancellationToken)
    {
        var counts = progress.CollisionCounts;
        async Task Report()
        {
            State.CollisionCounts = new Dictionary<ConstraintName, long>(counts);
            State.CollisionSamples = progress.CollisionSamples.ToDictionary(entry => entry.Key, entry => (IList<ConstraintCollisionSample>)entry.Value.ToList());
            State.CollisionSampleOverflowCounts = new Dictionary<ConstraintName, long>(progress.CollisionSampleOverflowCounts);
            if (counts.Count > 0)
            {
                var constraints = counts.OrderBy(entry => entry.Key.Value, StringComparer.Ordinal)
                    .Select(entry => $"{entry.Key}={entry.Value}");
                State.Progress.Message = $"Skipped {counts.Values.Sum()} duplicate claims: {string.Join(", ", constraints)}";
            }

            var written = await WriteStateAsync();
            if (written.TryGetException(out var error)) throw error;
        }

        var reported = await TryInvokeOnActivation(Report, cancellationToken);
        return reported.IsSuccess;
    }

    sealed class RebuildProgress
    {
        const int MaximumSamplesPerConstraint = 100;
        readonly byte[] _collisionHashKey = RandomNumberGenerator.GetBytes(32);

        public bool WroteAnything { get; set; }

        public EventSequenceNumber? ProcessedThrough { get; set; }

        public Dictionary<ConstraintName, long> CollisionCounts { get; } = [];

        public Dictionary<ConstraintName, IList<ConstraintCollisionSample>> CollisionSamples { get; } = [];

        public Dictionary<ConstraintName, long> CollisionSampleOverflowCounts { get; } = [];

        public void RecordCollision(ConstraintName name, string? claimedValue, EventSequenceNumber sequenceNumber)
        {
            CollisionCounts.TryGetValue(name, out var count);
            CollisionCounts[name] = count + 1;
            if (claimedValue is null) return;

            if (!CollisionSamples.TryGetValue(name, out var samples))
            {
                samples = [];
                CollisionSamples[name] = samples;
            }
            if (samples.Count == MaximumSamplesPerConstraint)
            {
                CollisionSampleOverflowCounts.TryGetValue(name, out var overflow);
                CollisionSampleOverflowCounts[name] = overflow + 1;
                return;
            }

            var hash = HMACSHA256.HashData(_collisionHashKey, Encoding.UTF8.GetBytes(claimedValue));
            samples.Add(new(Convert.ToHexString(hash).ToLowerInvariant(), sequenceNumber));
        }
    }
}
