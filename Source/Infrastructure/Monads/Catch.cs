// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using OneOf;
using OneOf.Types;

namespace Cratis.Chronicle.Monads;

/// <summary>
/// Represents the result of trying an execution that potentially produce an <see cref="Exception"/>.
/// </summary>
public class Catch : OneOfBase<None, Exception>
{
    Catch(OneOf<None, Exception> input)
        : base(input)
    {
    }

    /// <summary>
    /// Gets a value indicating whether the execution was successful.
    /// </summary>
    public bool IsSuccess => IsT0;

    public static implicit operator Catch(Exception error) => Failed(error);
    public static explicit operator Exception(Catch obj) => obj.AsT1;

    /// <summary>
    /// Creates a failed <see cref="Catch"/>.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <returns>The created <see cref="Catch"/>.</returns>
    public static Catch Failed(Exception error) => new(OneOf<None, Exception>.FromT1(error));

    /// <summary>
    /// Creates a successful <see cref="Catch"/>.
    /// </summary>
    /// <returns>The created <see cref="Catch"/>.</returns>
    public static Catch Success() => new(OneOf<None, Exception>.FromT0(default));

    /// <summary>
    /// Creates a successful <see cref="Catch{TResult}"/>.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="result">The result.</param>
    /// <returns>The created <see cref="Catch"/>.</returns>
    public static Catch<TResult> Success<TResult>(TResult result) => Catch<TResult>.Success(result);

    /// <summary>
    /// Creates a failed <see cref="Catch{TResult}"/>.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="error">The optional error.</param>
    /// <returns>The created <see cref="Result{T}"/>.</returns>
    public static Catch<TResult> Failed<TResult>(Exception error) => Catch<TResult>.Failed(error);

    /// <summary>
    /// Creates a failed <see cref="Catch{T, TErrorType}"/>.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <typeparam name="TError">The error type.</typeparam>
    /// <param name="error">The error.</param>
    /// <returns>The created <see cref="Result{T, TErrorType}"/>.</returns>
    public static Catch<TResult, TError> Failed<TResult, TError>(Exception error) => Catch<TResult, TError>.Failed(error);

    /// <summary>
    /// Creates a failed <see cref="Catch{T, TErrorType}"/>.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <typeparam name="TError">The error type.</typeparam>
    /// <param name="error">The error.</param>
    /// <returns>The created <see cref="Result{T, TErrorType}"/>.</returns>
    public static Catch<TResult, TError> Failed<TResult, TError>(TError error) => Catch<TResult, TError>.Failed(error);

    /// <summary>
    /// Creates a successful <see cref="Catch{T, TErrorType}"/>.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <typeparam name="TError">The error type.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>The created <see cref="Result{T, TErrorType}"/>.</returns>
    public static Catch<TResult, TError> Success<TResult, TError>(TResult value) => Catch<TResult, TError>.Success(value);

    /// <summary>
    /// Try to get the error.
    /// </summary>
    /// <param name="error">The optional error.</param>
    /// <returns>A boolean indicating whether the error was present.</returns>
    public bool TryGetException([NotNullWhen(true)]out Exception? error)
    {
        TryPickT1(out error, out _);
        return error is not null;
    }

    /// <summary>
    /// Rethrows the <see cref="Exception"/> error if any, preserving the correct stack trace.
    /// </summary>
    public void RethrowError()
    {
        if (TryGetException(out var error))
        {
            ExceptionDispatchInfo.Capture(error).Throw();
        }
    }
}
