// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Aggregates durable bucket progress independently of callback order.
/// </summary>
public static class ReplayBucketProgress
{
    /// <summary>
    /// Records a bucket result, retaining its prior durable position when no result was returned.
    /// </summary>
    /// <param name="steps">The persisted bucket steps.</param>
    /// <param name="id">The completed or stopped step.</param>
    /// <param name="result">Its full or partial result.</param>
    /// <param name="options">The result serializer options.</param>
    public static void Record(IList<ReplayBucketStep> steps, JobStepId id, JobStepResult result, JsonSerializerOptions options)
    {
        var full = result.TryGetFullResult<HandleEventsForPartitionResult>(out var handled, out _, options);
        for (var index = 0; index < steps.Count; index++)
        {
            var step = steps[index];
            if (step.Id != id) continue;
            steps[index] = step with
            {
                Completed = full && handled is not null,
                LastHandledEventSequenceNumber = handled?.LastHandledEventSequenceNumber ?? step.LastHandledEventSequenceNumber
            };
            return;
        }
    }

    /// <summary>
    /// Returns the minimum unfinished position, or the maximum when every bucket completed.
    /// </summary>
    /// <param name="steps">The persisted bucket progress.</param>
    /// <returns>The safe position, or unavailable when an unfinished bucket has no checkpoint.</returns>
    public static EventSequenceNumber Position(IEnumerable<ReplayBucketStep> steps)
    {
        var unfinished = steps.Where(step => !step.Completed).ToArray();
        if (unfinished.Any(step => !step.LastHandledEventSequenceNumber.IsActualValue)) return EventSequenceNumber.Unavailable;
        if (unfinished.Length > 0) return unfinished.Min(step => step.LastHandledEventSequenceNumber)!;
        return steps.Select(step => step.LastHandledEventSequenceNumber)
            .Where(position => position.IsActualValue)
            .DefaultIfEmpty(EventSequenceNumber.Unavailable)
            .Max()!;
    }
}
