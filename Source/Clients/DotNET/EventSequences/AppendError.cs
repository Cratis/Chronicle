// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents the concept of an error that can occur during appending of events.
/// </summary>@
/// <param name="Value">Actual value.</param>
public record AppendError(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents the identifier for an unknown event type.
    /// </summary>
    public static readonly AppendError Unknown = string.Empty;

    /// <summary>
    /// Implicitly convert from string to <see cref="AppendError"/>.
    /// </summary>
    /// <param name="id">String to convert from.</param>
    public static implicit operator AppendError(string id) => new(id);
}
