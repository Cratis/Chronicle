// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Keeps abandoned deliveries fenced across recovery and subscription changes.
/// </summary>
public class DeliveryFences
{
    readonly Dictionary<Key, DeliveryFence> _fences = [];

    /// <summary>
    /// Holds a partition without shortening an existing hold.
    /// </summary>
    /// <param name="fence">The captured delivery fence.</param>
    public void Hold(DeliveryFence fence)
    {
        if (!_fences.TryGetValue(fence.Partition, out var current) || current.ReleaseBy < fence.ReleaseBy)
        {
            _fences[fence.Partition] = fence;
        }
    }

    /// <summary>
    /// Releases only the version that was drained.
    /// </summary>
    /// <param name="partition">The partition.</param>
    /// <param name="releaseBy">The drained version's deadline.</param>
    public void Release(Key partition, DateTimeOffset releaseBy)
    {
        if (_fences.TryGetValue(partition, out var current) && current.ReleaseBy == releaseBy)
        {
            _fences.Remove(partition);
        }
    }

    /// <summary>
    /// Checks whether a partition is still held.
    /// </summary>
    /// <param name="partition">The partition.</param>
    /// <param name="now">The current time.</param>
    /// <returns>Whether the fence is active.</returns>
    public bool IsHeld(Key partition, DateTimeOffset now)
    {
        if (!_fences.TryGetValue(partition, out var fence)) return false;
        if (fence.ReleaseBy > now) return true;
        _fences.Remove(partition);
        return false;
    }

    /// <summary>
    /// Gets a snapshot of active fences.
    /// </summary>
    /// <param name="now">The current time.</param>
    /// <returns>The active fences.</returns>
    public IEnumerable<DeliveryFence> Active(DateTimeOffset now)
    {
        foreach (var partition in _fences.Where(pair => pair.Value.ReleaseBy <= now).Select(pair => pair.Key).ToArray())
        {
            _fences.Remove(partition);
        }
        return _fences.Values.ToArray();
    }

    /// <summary>
    /// Restores timeout fences, holding legacy or invalid identities rather than draining replacement targets.
    /// </summary>
    /// <param name="failures">The persisted failures.</param>
    /// <param name="hold">The effective hold.</param>
    public void SeedFrom(FailedPartitions failures, TimeSpan hold)
    {
        if (hold <= TimeSpan.Zero) return;
        foreach (var failure in failures.Partitions)
        {
            var attempt = failure.Attempts.LastOrDefault(attempt => attempt.Kind == FailureKind.Timeout);
            if (attempt is null) continue;
            Hold(RestoreFence(failure.Partition, attempt, hold));
        }
    }

    static DeliveryFence RestoreFence(Key partition, FailedPartitionAttempt attempt, TimeSpan hold)
    {
        var context = attempt.DeliveryFence;
        var known = context?.Targets is { Count: > 0 } targets && targets.All(AbandonedDeliveryDrain.IsValid);
        return new(
            partition,
            known && context!.ReleaseBy is { } releaseBy ? releaseBy : attempt.Occurred + hold,
            known ? context!.Targets! : []);
    }
}
