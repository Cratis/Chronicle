// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Commands;

/// <summary>
/// Extension methods for working with <see cref="CommandResult"/>.
/// </summary>
public static class CommandResultExtensions
{
    /// <summary>
    /// Ensures the command executed successfully, throwing if it did not.
    /// </summary>
    /// <param name="result">The <see cref="CommandResult"/> to check.</param>
    /// <exception cref="CommandFailed">Thrown when the command did not succeed.</exception>
    public static void EnsureSuccess(this CommandResult result)
    {
        // A missing result is a failure like any other - the call produced no answer at all - and reporting it
        // as one keeps the caller from having to distinguish it from a null reference somewhere else entirely.
        if (result?.IsSuccess != true)
        {
            throw new CommandFailed(result);
        }
    }

    /// <summary>
    /// Awaits the command result and ensures the command executed successfully, throwing if it did not.
    /// </summary>
    /// <param name="resultTask">The task producing the <see cref="CommandResult"/> to check.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="CommandFailed">Thrown when the command did not succeed.</exception>
    public static async Task EnsureSuccess(this Task<CommandResult> resultTask)
    {
        var result = await resultTask.ConfigureAwait(false);
        result.EnsureSuccess();
    }
}
