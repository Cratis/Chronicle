// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Auditing;

/// <summary>
/// Represents a causation type.
/// </summary>
/// <param name="Name">String representing the name of the type.</param>
public record CausationType(string Name) : ConceptAs<string>(Name)
{
    /// <summary>
    /// Represents the root causation type.
    /// </summary>
    public static readonly CausationType Root = new("Root");

    /// <summary>
    /// Represents the unknown causation type.
    /// </summary>
    public static readonly CausationType Unknown = new("Unknown");
}
