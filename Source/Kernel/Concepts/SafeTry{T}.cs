// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using OneOf;

namespace Cratis.Chronicle.Storage;

/// <summary>
/// Represents the result of trying to get a single value that can have an optional <see cref="Exception"/> error.
/// </summary>
/// <typeparam name="T">The result type.</typeparam>
public class SafeTry<T> : OneOfBase<T, Exception>
{
    SafeTry(OneOf<T, Exception> input) : base(input)
    {
    }

    /// <summary>
    /// Gets whether the try was successful, meaning it has a result.
    /// </summary>
    public bool IsSuccess => IsT0;

    public static implicit operator SafeTry<T>(T value) => Success(value);
    public static implicit operator SafeTry<T>(Exception error) => Failed(error);
    public static explicit operator Exception(SafeTry<T> obj) => obj.AsT1;

    /// <summary>
    /// Creates a failed <see cref="Try{T}"/>.
    /// </summary>
    /// <param name="error">The optional error.</param>
    /// <returns>The created <see cref="Try{T}"/>.</returns>
    public static SafeTry<T> Failed(Exception error) => new(OneOf<T, Exception>.FromT1(error));

    /// <summary>
    /// Creates a successful <see cref="Try{T}"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The created <see cref="Try{T}"/>.</returns>
    public static SafeTry<T> Success(T value) => new(OneOf<T, Exception>.FromT0(value));

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
    public bool TryGetError(out Exception? error) => TryPickT1(out error, out _);
}
