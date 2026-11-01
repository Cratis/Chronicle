// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Registrations;

/// <summary>
/// Identifies the authenticated principal that registered an observer.
/// </summary>
/// <param name="Value">The stable principal identifier.</param>
public record OwnerPrincipal(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets the principal for legacy registrations without authenticated ownership metadata.
    /// </summary>
    public static readonly OwnerPrincipal Anonymous = new("[anonymous]");
}
