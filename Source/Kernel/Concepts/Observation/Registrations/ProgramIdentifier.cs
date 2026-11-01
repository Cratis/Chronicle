// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Registrations;

/// <summary>
/// Identifies the program owning a registration, independently of its authenticated principal.
/// </summary>
/// <param name="Value">The program identifier.</param>
public record ProgramIdentifier(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the owner of legacy registrations without program metadata.
    /// </summary>
    public static readonly ProgramIdentifier Unknown = new("[N/A]");

    /// <summary>
    /// Gets whether a program identifier was supplied.
    /// </summary>
    public bool IsKnown => !string.IsNullOrWhiteSpace(Value) && this != Unknown;
}
