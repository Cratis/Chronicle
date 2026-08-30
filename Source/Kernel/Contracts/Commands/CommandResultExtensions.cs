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

    /// <summary>
    /// Ensures the command executed successfully, returning its response or throwing if it did not.
    /// </summary>
    /// <param name="result">The <see cref="CommandResult{TResponse}"/> to check.</param>
    /// <typeparam name="TResponse">Type of response the command produces.</typeparam>
    /// <returns>The response produced by the command.</returns>
    /// <exception cref="CommandFailed">Thrown when the command did not succeed.</exception>
    /// <remarks>
    /// This is the RPC-level outcome - authorized, valid, no exception - not any domain-level success/failure the
    /// response itself carries (an <c>AppendResponse</c>'s own <c>IsSuccess</c>, say). The caller inspects that
    /// separately on the returned response.
    /// </remarks>
    public static TResponse EnsureSuccess<TResponse>(this CommandResult<TResponse> result)
    {
        // CommandResult<TResponse> deliberately does not derive from CommandResult (protobuf-net cannot model
        // inheritance through an open generic - see its own remarks), so CommandFailed's message needs the shared
        // fields copied across rather than the response passed directly.
        if (result?.IsSuccess != true)
        {
            throw new CommandFailed(result is null
                ? null
                : new CommandResult
                {
                    CorrelationId = result.CorrelationId,
                    IsAuthorized = result.IsAuthorized,
                    ValidationResults = result.ValidationResults,
                    ExceptionMessages = result.ExceptionMessages,
                    ExceptionStackTrace = result.ExceptionStackTrace,
                    AuthorizationFailureReason = result.AuthorizationFailureReason
                });
        }

        return result.Response;
    }

    /// <summary>
    /// Awaits the command result and ensures the command executed successfully, returning its response or throwing if it did not.
    /// </summary>
    /// <param name="resultTask">The task producing the <see cref="CommandResult{TResponse}"/> to check.</param>
    /// <typeparam name="TResponse">Type of response the command produces.</typeparam>
    /// <returns>The response produced by the command.</returns>
    /// <exception cref="CommandFailed">Thrown when the command did not succeed.</exception>
    public static async Task<TResponse> EnsureSuccess<TResponse>(this Task<CommandResult<TResponse>> resultTask)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.EnsureSuccess();
    }
}
