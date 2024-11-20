// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using OneOf;

namespace Cratis.Chronicle.Storage;

/// <summary>
/// Represents an optional value.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
[GenerateOneOf]
public partial class SafeOption<T> : OneOfBase<T, SafeTry>
{
    /// <summary>
    /// Gets whether value is present.
    /// </summary>
    public bool HasValue => IsT0;

    /// <summary>
    /// Creates a none <see cref="SafeOption{T}"/>.
    /// </summary>
    /// <returns>The created <see cref="SafeOption{T}" />.</returns>
    public static SafeOption<T> None() => SafeTry.Success();

    /// <summary>
    /// Creates a none <see cref="SafeOption{T}"/>.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <returns>The created <see cref="SafeOption{T}" />.</returns>
    public static SafeOption<T> Error(Exception error) => SafeTry.Failed(error);

    /// <summary>
    /// Try to get the value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>A boolean indicating whether the value is present.</returns>
    public bool TryGetValue([NotNullWhen(true)]out T value) => TryPickT0(out value, out _);

    /// <summary>
    /// Try to get the error.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <returns>A boolean indicating whether the error is present.</returns>
    public bool TryGetError([NotNullWhen(true)]out Exception? error)
    {
        error = default;
        return TryPickT1(out var maybeError, out _) && maybeError.TryGetError(out error);
    }
}
