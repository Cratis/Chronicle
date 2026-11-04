// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.V2;

/// <summary>
/// Identifies a unique-constraint encoding contract.
/// </summary>
/// <param name="Value">The encoding version.</param>
internal record HashVersion(int Value) : ConceptAs<int>(Value)
{
    /// <summary>
    /// Gets the keyed, position-preserving encoding version.
    /// </summary>
    public static readonly HashVersion V2 = new(2);
}
