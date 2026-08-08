// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Commands;

/// <summary>
/// The exception that is thrown when a command executed against the kernel did not succeed.
/// </summary>
/// <param name="result">The <see cref="CommandResult"/> describing the failure, or null when the call produced no result.</param>
public class CommandFailed(CommandResult? result)
    : Exception(BuildMessage(result))
{
    /// <summary>
    /// Gets the <see cref="CommandResult"/> describing the failure, or null when the call produced no result.
    /// </summary>
    public CommandResult? Result { get; } = result;

    static string BuildMessage(CommandResult? result)
    {
        if (result is null)
        {
            return "Command failed: no result was returned";
        }

        var reasons = result.ValidationResults.Select(_ => _.Message)
            .Concat(result.ExceptionMessages);
        if (!result.IsAuthorized)
        {
            reasons = reasons.Prepend(string.IsNullOrEmpty(result.AuthorizationFailureReason) ? "Unauthorized" : result.AuthorizationFailureReason);
        }

        return $"Command failed: {string.Join(", ", reasons)}";
    }
}
