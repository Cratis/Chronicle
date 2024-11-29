// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using OneOf;

namespace Cratis.Chronicle.Concepts;

/// <summary>
/// Represents the result of trying an execution that can return a result, an error or potentially produce an <see cref="Exception"/>.
/// </summary>
/// <typeparam name="TResult">The result type.</typeparam>
/// <typeparam name="TError">The error type.</typeparam>
public class Catch<TResult, TError> : OneOfBase<TResult, TError, Exception>
{
    Catch(OneOf<TResult, TError, Exception> input)
        : base(input)
    {
    }

    /// <summary>
    /// Gets a value indicating whether the execution was successful, meaning it has a result.
    /// </summary>
    public bool IsSuccess => IsT0;

    public static implicit operator Catch<TResult, TError>(TResult value) => Success(value);
    public static implicit operator Catch<TResult, TError>(Exception error) => Failed(error);
    public static implicit operator Catch<TResult, TError>(TError errorType) => Failed(errorType);

    public static explicit operator TResult(Catch<TResult, TError> obj) => obj.AsT0;
    public static explicit operator TError(Catch<TResult, TError> obj) => obj.AsT1;
    public static explicit operator Exception(Catch<TResult, TError> obj) => obj.AsT2;

    /// <summary>
    /// Creates a failed <see cref="Catch{T, TErrorType}"/>.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <returns>The created <see cref="Result{T, TErrorType}"/>.</returns>
    public static Catch<TResult, TError> Failed(Exception error) => new(OneOf<TResult, TError, Exception>.FromT2(error));

    /// <summary>
    /// Creates a failed <see cref="Catch{T, TErrorType}"/>.
    /// </summary>
    /// <param name="error">The error type.</param>
    /// <returns>The created <see cref="Result{T, TErrorType}"/>.</returns>
    public static Catch<TResult, TError> Failed(TError error) => new(OneOf<TResult, TError, Exception>.FromT1(error));

    /// <summary>
    /// Creates a successful <see cref="Catch{T, TErrorType}"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The created <see cref="Result{T, TErrorType}"/>.</returns>
    public static Catch<TResult, TError> Success(TResult value) => new(OneOf<TResult, TError, Exception>.FromT0(value));

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
    public bool TryGetError([NotNullWhen(true)]out Exception? error) => TryPickT2(out error, out _);

    /// <summary>
    /// Rethrows the <see cref="Exception"/> error if any, preserving the correct stack trace.
    /// </summary>
    public void RethrowError()
    {
        if (TryGetError(out var error))
        {
            ExceptionDispatchInfo.Capture(error).Throw();
        }
    }
}
