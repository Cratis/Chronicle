// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using OneOf;
using OneOf.Types;

namespace Cratis.Chronicle.Concepts;

/// <summary>
/// Represents the result of trying an execution that potentially produce an <see cref="Exception"/>.
/// </summary>
public class Catch : OneOfBase<None, Exception>
{
    Catch(OneOf<None, Exception> input) : base(input)
    {
    }

    /// <summary>
    /// Gets whether the execution was successful.
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
