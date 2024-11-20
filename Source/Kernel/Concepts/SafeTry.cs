// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using OneOf;
using OneOf.Types;

namespace Cratis.Chronicle.Storage;

/// <summary>
/// Represents the result of trying to get a single value that can have an optional <see cref="Exception"/> error.
/// </summary>
public class SafeTry : OneOfBase<None, Exception>
{
    SafeTry(OneOf<None, Exception> input) : base(input)
    {
    }

    /// <summary>
    /// Gets whether the try was successful, meaning it has a result.
    /// </summary>
    public bool IsSuccess => IsT0;

    public static implicit operator SafeTry(Exception error) => Failed(error);
    public static explicit operator Exception(SafeTry obj) => obj.AsT1;

    /// <summary>
    /// Creates a failed <see cref="SafeTry"/>.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <returns>The created <see cref="SafeTry"/>.</returns>
    public static SafeTry Failed(Exception error) => new(OneOf<None, Exception>.FromT1(error));

    /// <summary>
    /// Creates a successful <see cref="SafeTry"/>.
    /// </summary>
    /// <returns>The created <see cref="SafeTry"/>.</returns>
    public static SafeTry Success() => new(OneOf<None, Exception>.FromT0(default));

    /// <summary>
    /// Try to get the error.
    /// </summary>
    /// <param name="error">The optional error.</param>
    /// <returns>A boolean indicating whether the error was present.</returns>
    public bool TryGetError([NotNullWhen(true)]out Exception? error)
    {
        TryPickT1(out error, out _);
        return error is not null;
    }
}
