// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

/// <summary>
/// Represents the concept of a reason for redacting an event.
/// </summary>
/// <param name="Value">Inner value.</param>
public record RedactionReason(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Represents the unknown redaction reason.
    /// </summary>
    public static readonly RedactionReason Unknown = new("Unknown");

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="RedactionReason"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator RedactionReason(string value) => new(value);
}
