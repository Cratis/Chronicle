// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Monads;

/// <summary>
/// Convenience methods for creating Result instances.
/// </summary>
public class Result
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> class.
    /// </summary>
    /// <param name="success">Whether the result was successful.</param>
    Result(bool success) => IsSuccess = success;

    /// <summary>
    /// Gets a value indicating whether the execution was successful.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Creates a failed <see cref="Result"/>.
    /// </summary>
    /// <returns>The created <see cref="Result"/>.</returns>
    public static Result Failed() => new(false);

    /// <summary>
    /// Creates a successful <see cref="Result"/>.
    /// </summary>
    /// <returns>The created <see cref="Result"/>.</returns>
    public static Result Success() => new(true);

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
