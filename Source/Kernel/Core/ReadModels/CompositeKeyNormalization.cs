// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Dynamic;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// The outcome of normalizing a composite key, either the typed key or the reason it was refused.
/// </summary>
/// <param name="Key">The typed parts of the key, or null if the key was refused.</param>
/// <param name="Message">The reason the key was refused, or null if it was accepted.</param>
public record CompositeKeyNormalization(ExpandoObject? Key, string? Message)
{
    /// <summary>
    /// Gets a value indicating whether the key was accepted.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Key))]
    [MemberNotNullWhen(false, nameof(Message))]
    public bool IsValid => Key is not null;

    /// <summary>
    /// Creates the outcome for an accepted key.
    /// </summary>
    /// <param name="key">The typed parts of the key.</param>
    /// <returns>The <see cref="CompositeKeyNormalization"/>.</returns>
    public static CompositeKeyNormalization Valid(ExpandoObject key) => new(key, null);

    /// <summary>
    /// Creates the outcome for a refused key.
    /// </summary>
    /// <param name="message">The reason the key was refused.</param>
    /// <returns>The <see cref="CompositeKeyNormalization"/>.</returns>
    public static CompositeKeyNormalization Invalid(string message) => new(null, message);
}
