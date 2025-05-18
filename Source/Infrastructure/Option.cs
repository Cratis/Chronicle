// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using OneOf;
using OneOf.Types;

namespace Cratis.Chronicle;

/// <summary>
/// Represents an optional value.
/// </summary>
/// <typeparam name="TValue">The type of the value.</typeparam>
[GenerateOneOf]
public partial class Option<TValue> : OneOfBase<TValue, None>
{
    /// <summary>
    /// Gets whether value is present.
    /// </summary>
    public bool HasValue => IsT0;

    /// <summary>
    /// Creates a none <see cref="Option{T}"/>.
    /// </summary>
    /// <returns>The created <see cref="Option{T}"/>.</returns>
    public static Option<TValue> None() => default(None);

    /// <summary>
    /// Try to get the value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// /// /// /// <returns>A boolean indicating whether the value is present.</returns>
    public bool TryGetValue([NotNullWhen(true)]out TValue value) => TryPickT0(out value, out _);
}
