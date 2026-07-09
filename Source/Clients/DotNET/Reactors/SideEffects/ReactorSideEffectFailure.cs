// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Represents a failure that occurred during reactor side-effect processing.
/// </summary>
/// <param name="AppendFailures">Collection of append failures that occurred.</param>
public record ReactorSideEffectFailure(IEnumerable<AppendFailure> AppendFailures)
{
    /// <summary>
    /// Gets messages describing the side-effect append failures.
    /// </summary>
    /// <returns>A collection of failure messages.</returns>
    public IEnumerable<string> GetMessages()
    {
        var appendFailures = AppendFailures.ToList();
        for (var failureIndex = 0; failureIndex < appendFailures.Count; failureIndex++)
        {
            var failure = appendFailures[failureIndex];
            var failureNumber = failureIndex + 1;
            var hasDetails = false;

            var targetEventSourceIds = failure.TargetEventSourceIds.ToList();
            if (targetEventSourceIds.Count > 0)
            {
                hasDetails = true;
                var formattedIds = string.Join(", ", targetEventSourceIds.Select(id => $"'{id}'"));
                yield return $"Append failure {failureNumber}: Failed appending to event source id(s) {formattedIds}";
            }

            foreach (var constraintViolation in failure.ConstraintViolations)
            {
                hasDetails = true;
                yield return $"Append failure {failureNumber}: Constraint violation for event type '{constraintViolation.EventTypeId}': {constraintViolation.Message}";
            }

            if (failure.HasConcurrencyViolation)
            {
                hasDetails = true;
                yield return $"Append failure {failureNumber}: Concurrency violation";
            }

            foreach (var error in failure.Errors)
            {
                hasDetails = true;
                yield return $"Append failure {failureNumber}: {error}";
            }

            if (!hasDetails)
            {
                yield return $"Append failure {failureNumber}: Unknown append failure";
            }
        }
    }

    /// <summary>
    /// Gets the distinct <see cref="EventSourceId"/> the failed side-effect events were being appended to.
    /// </summary>
    /// <returns>The collection of target <see cref="EventSourceId"/>.</returns>
    public IEnumerable<EventSourceId> GetTargetEventSourceIds() =>
        AppendFailures.SelectMany(failure => failure.TargetEventSourceIds).Distinct();

    /// <summary>
    /// Creates a <see cref="ReactorSideEffectFailure"/> from a failed append operation.
    /// </summary>
    /// <param name="appendResult">The failed <see cref="IAppendResult"/> (e.g. <see cref="AppendResult"/> or <see cref="AppendManyResult"/>).</param>
    /// <param name="targetEventSourceIds">The <see cref="EventSourceId"/> the failed side-effect events were being appended to.</param>
    /// <returns>A <see cref="ReactorSideEffectFailure"/> containing the append failure details.</returns>
    public static ReactorSideEffectFailure FromAppendResult(IAppendResult appendResult, IEnumerable<EventSourceId> targetEventSourceIds)
    {
        var constraintViolations = appendResult.ConstraintViolations.Select(cv =>
            new ReactorConstraintViolation(cv.EventTypeId, cv.Message));
        var errors = appendResult.Errors.Select(e => e.Value);

        var failure = new AppendFailure(
            constraintViolations,
            appendResult.HasConcurrencyViolations,
            errors,
            targetEventSourceIds.Distinct().ToList());

        return new([failure]);
    }
}
