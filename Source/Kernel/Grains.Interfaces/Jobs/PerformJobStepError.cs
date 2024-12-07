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
public record PerformJobStepError(object? PartialResult, IEnumerable<string>? ErrorMessages, string? ExceptionStackTrace)
{
    /// <summary>
    /// Gets whether there is a partial result.
    /// </summary>
    public bool HasPartialResult => PartialResult is not null;

    /// <summary>
    /// Try to get the partial result.
    /// </summary>
    /// <param name="result">The outputted partial <typeparamref name="TResult"/> result.</param>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <returns>True if present, false if not.</returns>
    public bool TryGetPartialResult<TResult>([NotNullWhen(true)]out TResult? result)
        where TResult : class
    {
        result = PartialResult as TResult;
        return result is not null;
    }
}