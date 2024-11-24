// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using OneOf;

namespace Cratis.Chronicle.Concepts;

/// <summary>
/// Represents the result of trying an execution that can return a result or potentially produce an <see cref="Exception"/>.
/// </summary>
/// <typeparam name="TResult">The result type.</typeparam>
public class Catch<TResult> : OneOfBase<TResult, Exception>
{
    Catch(OneOf<TResult, Exception> input) : base(input)
    {
    }

    /// <summary>
    /// Gets whether the execution was successful, meaning it has a result.
    /// </summary>
    public bool IsSuccess => IsT0;

    public static implicit operator Catch<TResult>(TResult value) => Success(value);
    public static implicit operator Catch<TResult>(Exception error) => Failed(error);
    public static explicit operator Exception(Catch<TResult> obj) => obj.AsT1;
    public static explicit operator TResult(Catch<TResult> obj) => obj.AsT0;

    /// <summary>
    /// Creates a failed <see cref="Result{T}"/>.
    /// </summary>
    /// <param name="error">The optional error.</param>
    /// <returns>The created <see cref="Result{T}"/>.</returns>
    public static Catch<TResult> Failed(Exception error) => new(OneOf<TResult, Exception>.FromT1(error));

    /// <summary>
    /// Creates a successful <see cref="Result{T}"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The created <see cref="Result{T}"/>.</returns>
    public static Catch<TResult> Success(TResult value) => new(OneOf<TResult, Exception>.FromT0(value));

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
    public bool TryGetError(out Exception? error) => TryPickT1(out error, out _);
}
