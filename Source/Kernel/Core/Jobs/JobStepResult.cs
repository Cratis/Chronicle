// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Cratis.Chronicle.Json;
using Cratis.Monads;

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Represents the result of performing a job step.
/// </summary>
/// <param name="result">The result.</param>
[GenerateSerializer]
public class JobStepResult(Result<object?, PerformJobStepError> result)
{
    [Id(0)]
    readonly Result<object?, PerformJobStepError> _result = result;

    /// <summary>
    /// Creates a succeeded <see cref="JobStepResult"/>.
    /// </summary>
    /// <param name="input">The optional result object.</param>
    /// <returns>The <see cref="JobStepResult"/>.</returns>
    public static JobStepResult Succeeded(object? input = default) => new(Result.Success<object?, PerformJobStepError>(input));

    /// <summary>
    /// Creates a failed <see cref="JobStepResult"/>.
    /// </summary>
    /// <param name="input">The error messages.</param>
    /// <returns>The <see cref="JobStepResult"/>.</returns>
    public static JobStepResult Failed(PerformJobStepError input) => new(Result<object?, PerformJobStepError>.Failed(input));

    /// <summary>
    /// Creates a failed <see cref="JobStepResult"/>.
    /// </summary>
    /// <param name="messages">The error messages.</param>
    /// <returns>The <see cref="JobStepResult"/>.</returns>
    public static JobStepResult Failed(params string[] messages) => new(PerformJobStepError.Failed(messages, string.Empty));

    /// <summary>
    /// Try to get the full result, if not it tries to get the partial result.
    /// </summary>
    /// <param name="result">The optional full or partial result.</param>
    /// <param name="error">The optional <see cref="PerformJobStepError"/> error when the result was partial or fully failed.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for deserialization.</param>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <returns>True if full result, false if partial or none result.</returns>
    public bool TryGetFullResult<TResult>([NotNullWhen(true)] out TResult? result, [NotNullWhen(false)] out PerformJobStepError? error, JsonSerializerOptions jsonSerializerOptions)
        where TResult : class
    {
        error = null;
        if (_result.TryGetResult(out var fullResult))
        {
            result = fullResult.DeserializeIfNecessary<TResult>(jsonSerializerOptions)!;
            return true;
        }

        error = _result.AsT1;
        result = error.PartialResult?.DeserializeIfNecessary<TResult>(jsonSerializerOptions);
        return false;
    }

    /// <summary>
    /// Try to get the error.
    /// </summary>
    /// <param name="error">The optional error.</param>
    /// <returns>True if error, false if not.</returns>
    public bool TryGetError([NotNullWhen(true)] out PerformJobStepError? error) => _result.TryGetError(out error);
}
