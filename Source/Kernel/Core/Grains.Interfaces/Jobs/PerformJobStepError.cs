// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// Represents the error of performing a <see cref="IJobStep"/>.
/// </summary>
/// <param name="PartialResult">The optional partial result.</param>
/// <param name="ErrorMessages">The error messages.</param>
/// <param name="ExceptionStackTrace">The optional exception stack trace.</param>
/// <param name="Cancelled">Whether the job step was cancelled.</param>
[GenerateSerializer]
public record PerformJobStepError(object? PartialResult, IList<string>? ErrorMessages, string? ExceptionStackTrace, bool Cancelled)
{
    static readonly IList<string> _cancelledErrorMessage = ["Job step task was cancelled"];

    /// <summary>
    /// Gets whether there is a partial result.
    /// </summary>
    public bool HasPartialResult => PartialResult is not null;

    /// <summary>
    /// Creates <see cref="PerformJobStepError"/> for a completely failed job step.
    /// </summary>
    /// <param name="errorMessages">The error messages.</param>
    /// <param name="exceptionStackTrace">The optional exception stack trace.</param>
    /// <returns>The <see cref="PerformJobStepError"/>.</returns>
    public static PerformJobStepError Failed(IEnumerable<string> errorMessages, string? exceptionStackTrace) =>
        new(Enumerable.Empty<string>(), errorMessages.ToList(), exceptionStackTrace, false);

    /// <summary>
    /// Creates <see cref="PerformJobStepError"/> for a completely failed job step.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>The <see cref="PerformJobStepError"/>.</returns>
    public static PerformJobStepError Failed(Exception exception) =>
        new(default, exception.GetAllMessages().ToList(), exception.StackTrace, false);

    /// <summary>
    /// Creates <see cref="PerformJobStepError"/> for a partially failed job step.
    /// </summary>
    /// <param name="partialResult">The partial result.</param>
    /// <param name="errorMessages">The error messages.</param>
    /// <param name="exceptionStackTrace">The optional exception stack trace.</param>
    /// <returns>The <see cref="PerformJobStepError"/>.</returns>
    public static PerformJobStepError FailedWithPartialResult(object partialResult, IEnumerable<string> errorMessages, string? exceptionStackTrace) =>
        new(partialResult, errorMessages.ToList(), exceptionStackTrace, false);

    /// <summary>
    /// Creates <see cref="PerformJobStepError"/> for a partially failed job step.
    /// </summary>
    /// <param name="partialResult">The partial result.</param>
    /// <param name="exception">The exception.</param>
    /// <returns>The <see cref="PerformJobStepError"/>.</returns>
    public static PerformJobStepError FailedWithPartialResult(object partialResult, Exception exception) =>
        FailedWithPartialResult(partialResult, exception.GetAllMessages(), exception.StackTrace);

    /// <summary>
    /// Creates <see cref="PerformJobStepError"/> for a cancelled job step with partial result.
    /// </summary>
    /// <param name="partialResult">The partial result.</param>
    /// <returns>The <see cref="PerformJobStepError"/>.</returns>
    public static PerformJobStepError CancelledWithPartialResult(object partialResult) =>
        new(partialResult, _cancelledErrorMessage, default, true);

    /// <summary>
    /// Creates <see cref="PerformJobStepError"/> for a cancelled job step with partial result.
    /// </summary>
    /// <returns>The <see cref="PerformJobStepError"/>.</returns>
    public static PerformJobStepError CancelledWithNoResult() =>
        new(default, _cancelledErrorMessage, default, true);

    /// <summary>
    /// Try to get the partial result.
    /// </summary>
    /// <param name="result">The outputted partial <typeparamref name="TResult"/> result.</param>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <returns>True if present, false if not.</returns>
    public bool TryGetPartialResult<TResult>([NotNullWhen(true)] out TResult? result)
        where TResult : class
    {
        result = PartialResult as TResult;
        return result is not null;
    }
}