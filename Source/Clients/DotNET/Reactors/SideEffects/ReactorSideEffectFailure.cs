// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Represents a failure that occurred during reactor side-effect processing.
/// </summary>
/// <param name="AppendFailures">Collection of append failures that occurred.</param>
public record ReactorSideEffectFailure(IEnumerable<AppendFailure> AppendFailures)
{
    /// <summary>
    /// Creates a <see cref="ReactorSideEffectFailure"/> from a single <see cref="AppendResult"/>.
    /// </summary>
    /// <param name="appendResult">The failed <see cref="AppendResult"/>.</param>
    /// <returns>A <see cref="ReactorSideEffectFailure"/> containing the append failure details.</returns>
    public static ReactorSideEffectFailure FromAppendResult(AppendResult appendResult)
    {
        var constraintViolations = appendResult.ConstraintViolations.Select(cv =>
            new ReactorConstraintViolation(cv.EventTypeId, cv.Message));
        var errors = appendResult.Errors.Select(e => e.Value);

        var failure = new AppendFailure(
            constraintViolations,
            appendResult.HasConcurrencyViolations,
            errors);

        return new([failure]);
    }
}
