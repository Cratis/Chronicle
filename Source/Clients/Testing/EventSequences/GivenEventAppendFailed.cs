// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// The exception that is thrown when an event given to a scenario is refused by the in-process kernel, which would
/// otherwise leave a specification running against history it never received.
/// </summary>
/// <param name="eventType">The CLR type of the event that was refused.</param>
/// <param name="result">The <see cref="IAppendResult"/> describing why the append was refused.</param>
public sealed class GivenEventAppendFailed(Type eventType, IAppendResult result)
    : Exception(BuildMessage(eventType, result))
{
    static string BuildMessage(Type eventType, IAppendResult result)
    {
        var message = new StringBuilder($"The given event '{eventType.Name}' was refused and is not part of the scenario's history.");

        foreach (var violation in result.ConstraintViolations)
        {
            message.AppendLine().Append($"Constraint violation [{violation.ConstraintType}] {violation.ConstraintName}: {violation.Message}");
        }

        foreach (var error in result.Errors)
        {
            message.AppendLine().Append($"Error: {error}");
        }

        if (result.HasConcurrencyViolations)
        {
            message.AppendLine().Append("Concurrency violations occurred.");
        }

        return message.ToString();
    }
}
