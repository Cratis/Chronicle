// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Auditing;

/// <summary>
/// Represents a causation type.
/// </summary>
/// <param name="Name">String representing the name of the type.</param>
/// <param name="IsReusable">Whether or not instances of a causation can be reused if properties are equal.</param>
public record CausationType(string Name, bool IsReusable = false)
{
    /// <summary>
    /// Represents the root causation type.
    /// </summary>
    public static readonly CausationType Root = new("Root", true);

    /// <summary>
    /// Represents the unknown causation type.
    /// </summary>
    public static readonly CausationType Unknown = new("Unknown", true);
}
