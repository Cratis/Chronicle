// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// The exception that is thrown when an append is rejected by the kernel.
/// </summary>
/// <param name="reasons">The reasons the append was rejected.</param>
public class AppendRejected(IEnumerable<string> reasons)
    : Exception($"The append was rejected: {string.Join(", ", reasons)}")
{
    /// <summary>
    /// Throws when an append response carries errors or constraint violations.
    /// </summary>
    /// <param name="errors">The errors the append reported.</param>
    /// <param name="constraintViolations">The constraint violations the append reported.</param>
    /// <exception cref="AppendRejected">Thrown when there is anything to report.</exception>
    internal static void ThrowIfRejected(
        IEnumerable<AppendError> errors,
        IEnumerable<ConstraintViolation> constraintViolations)
    {
        var reasons = errors.Select(error => error.Value)
            .Concat(constraintViolations.Select(violation => violation.Message.Value))
            .ToArray();

        if (reasons.Length == 0)
        {
            return;
        }

        throw new AppendRejected(reasons);
    }
}
