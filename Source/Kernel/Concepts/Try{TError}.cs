// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using OneOf;
using OneOf.Types;

namespace Cratis.Chronicle.Storage;

/// <summary>
/// Represents the result of trying to get a single value that can have an optional <see cref="Exception"/> error.
/// </summary>
/// <typeparam name="TError">The type of the error tyoe.</typeparam>
public class Try<TError> : OneOfBase<None, TError>
    where TError : Enum
{
    Try(OneOf<None, TError> input) : base(input)
    {
    }

    /// <summary>
    /// Gets whether the try was successful, meaning it has a result.
    /// </summary>
    public bool IsSuccess => IsT0;

    public static implicit operator Try<TError>(TError error) => Failed(error);

    public static explicit operator TError(Try<TError> obj) => obj.AsT1;

    /// <summary>
    /// Creates a failed <see cref="Try{T}"/>.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <returns>The created <see cref="Try{T}"/>.</returns>
    public static Try<TError> Failed(TError error) => new(OneOf<None, TError>.FromT1(error));

    /// <summary>
    /// Creates a successful <see cref="Try{T}"/>.
    /// </summary>
    /// <returns>The created <see cref="Try{T}"/>.</returns>
    public static Try<TError> Success() => new(OneOf<None, TError>.FromT0(default));

    /// <summary>
    /// Try to get the error.
    /// </summary>
    /// <param name="error">The optional error.</param>
    /// <returns>A boolean indicating whether the error was present.</returns>
    public bool TryGetError([NotNullWhen(true)] out TError? error) => TryPickT1(out error, out _);
}
