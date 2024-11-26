// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using OneOf;

namespace Cratis.Chronicle.Concepts;

/// <summary>
/// Represents the result of trying an execution that can return a result or an error.
/// </summary>
/// <typeparam name="TResult">The result type.</typeparam>
/// <typeparam name="TError">The error type.</typeparam>
public class Result<TResult, TError> : OneOfBase<TResult, TError>
{
    Result(OneOf<TResult, TError> input) : base(input)
    {
    }

    /// <summary>
    /// Gets whether the execution was successful, meaning it has a result.
    /// </summary>
    public bool IsSuccess => IsT0;

    public static implicit operator Result<TResult, TError>(TResult value) => Success(value);
    public static implicit operator Result<TResult, TError>(TError error) => Failed(error);

    public static explicit operator TResult(Result<TResult, TError> obj) => obj.AsT0;
    public static explicit operator TError(Result<TResult, TError> obj) => obj.AsT1;

    /// <summary>
    /// Creates a failed <see cref="Result{T}"/>.
    /// </summary>
    /// <param name="error">The optional error.</param>
    /// <returns>The created <see cref="Result{T}"/>.</returns>
    public static Result<TResult, TError> Failed(TError error) => new(OneOf<TResult, TError>.FromT1(error));

    /// <summary>
    /// Creates a successful <see cref="Result{T}"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The created <see cref="Result{T}"/>.</returns>
    public static Result<TResult, TError> Success(TResult value) => new(OneOf<TResult, TError>.FromT0(value));

    /// <summary>
    /// Try to get the result <typeparamref name="TResult"/>.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <returns>A boolean indicating whether the result was present.</returns>
    public bool TryGetResult([NotNullWhen(true)] out TResult result) => TryPickT0(out result, out _);

    /// <summary>
    /// Try to get the error.
    /// </summary>
    /// <param name="error">The optional error.</param>
    /// <returns>A boolean indicating whether the error was present.</returns>
    public bool TryGetError(out TError error) => TryPickT1(out error, out _);
}
