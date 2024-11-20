// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using OneOf;

namespace Cratis.Chronicle.Concepts;

/// <summary>
/// Represents the result of trying to get a single value that can have an optional <see cref="Exception"/> error.
/// </summary>
/// <typeparam name="T">The result type.</typeparam>
/// <typeparam name="TError">The error type.</typeparam>
public class Try<T, TError> : OneOfBase<T, TError>
{
    Try(OneOf<T, TError> input) : base(input)
    {
    }

    /// <summary>
    /// Gets whether the try was successful, meaning it has a result.
    /// </summary>
    public bool IsSuccess => IsT0;

    public static implicit operator Try<T, TError>(T value) => Success(value);
    public static implicit operator Try<T, TError>(TError error) => Failed(error);
    public static explicit operator T(Try<T, TError> obj) => obj.AsT0;
    public static explicit operator TError(Try<T, TError> obj) => obj.AsT1;

    /// <summary>
    /// Creates a failed <see cref="Try{T}"/>.
    /// </summary>
    /// <param name="error">The optional error.</param>
    /// <returns>The created <see cref="Try{T}"/>.</returns>
    public static Try<T, TError> Failed(TError error) => new(OneOf<T, TError>.FromT1(error));

    /// <summary>
    /// Creates a successful <see cref="Try{T}"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The created <see cref="Try{T}"/>.</returns>
    public static Try<T, TError> Success(T value) => new(OneOf<T, TError>.FromT0(value));

    /// <summary>
    /// Try to get the result <typeparamref name="T"/>.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <returns>A boolean indicating whether the result was present.</returns>
    public bool TryGetResult([NotNullWhen(true)] out T result) => TryPickT0(out result, out _);

    /// <summary>
    /// Try to get the error.
    /// </summary>
    /// <param name="error">The optional error.</param>
    /// <returns>A boolean indicating whether the error was present.</returns>
    public bool TryGetError(out TError error) => TryPickT1(out error, out _);
}
