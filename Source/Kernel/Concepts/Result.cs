// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts;

/// <summary>
/// Convenience methods for creating Result instances.
/// </summary>
public static class Result
{
    /// <summary>
    /// Creates a failed <see cref="Result{T}"/>.
    /// </summary>
    /// <typeparam name="TError">The type of the error.</typeparam>
    /// <param name="error">The error.</param>
    /// <returns>The created <see cref="Result{T}"/>.</returns>
    public static Result<TError> Failed<TError>(TError error) => Result<TError>.Failed(error);

    /// <summary>
    /// Creates a successful <see cref="Result{T}"/>.
    /// </summary>
    /// <typeparam name="TError">The type of the error.</typeparam>
    /// <returns>The created <see cref="Result{T}"/>.</returns>
    public static Result<TError> Success<TError>() => Result<TError>.Success();

    /// <summary>
    /// Creates a failed <see cref="Result{T}"/>.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <typeparam name="TError">The error type.</typeparam>
    /// <param name="error">The error.</param>
    /// <returns>The created <see cref="Result{T}"/>.</returns>
    public static Result<TResult, TError> Failed<TResult, TError>(TError error) => Result<TResult, TError>.Failed(error);

    /// <summary>
    /// Creates a successful <see cref="Result{T}"/>.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <typeparam name="TError">The error type.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>The created <see cref="Result{T}"/>.</returns>
    public static Result<TResult, TError> Success<TResult, TError>(TResult value) => Result<TResult, TError>.Success(value);
}
