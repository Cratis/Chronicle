// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Exception that is thrown when a reactor side-effect append operation fails.
/// </summary>
/// <remarks>
/// This exception is used to fail a reactor partition when an event append operation
/// in a side-effect handler encounters constraint violations, concurrency violations, or errors.
/// </remarks>
/// <param name="result">The <see cref="AppendResult"/> that failed.</param>
public class ReactorAppendFailedException(AppendResult result) : Exception(BuildMessage(result))
{
    /// <summary>
    /// Gets the <see cref="AppendResult"/> that caused the failure.
    /// </summary>
    public AppendResult AppendResult { get; } = result;

    static string BuildMessage(AppendResult result)
    {
        var message = new StringBuilder("Reactor side-effect append operation failed.");

        if (result.HasConstraintViolations)
        {
            message.AppendLine().Append($"Constraint violations ({result.ConstraintViolations.Count()}):");
            foreach (var violation in result.ConstraintViolations)
            {
                message.AppendLine().Append($"  - EventType {violation.EventTypeId}: {violation.Message}");
            }
        }

        if (result.HasConcurrencyViolations)
        {
            message.AppendLine().Append("Concurrency violation occurred.");
        }

        if (result.HasErrors)
        {
            message.AppendLine().Append($"Errors ({result.Errors.Count()}):");
            foreach (var error in result.Errors)
            {
                message.AppendLine().Append($"  - {error.Value}");
            }
        }

        return message.ToString();
    }
}
