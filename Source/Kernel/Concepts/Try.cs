// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using OneOf;
using OneOf.Types;

namespace Cratis.Chronicle.Storage;

/// <summary>
/// Represents the result of trying to get a single value that can have an optional <see cref="Exception"/> error.
/// </summary>
public class Try : OneOfBase<None, Exception>
{
    Try(OneOf<None, Exception> input) : base(input)
    {
    }

    /// <summary>
    /// Gets whether the try was successful, meaning it has a result.
    /// </summary>
    public bool IsSuccess => IsT0;

    /// <summary>
    /// Creates a failed <see cref="Try"/>.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <returns>The created <see cref="Try"/>.</returns>
    public static Try Failed(Exception error) => new(OneOf<None, Exception>.FromT1(error));

    /// <summary>
    /// Creates a successful <see cref="Try"/>.
    /// </summary>
    /// <returns>The created <see cref="Try"/>.</returns>
    public static Try Success() => new(OneOf<None, Exception>.FromT0(default));

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
