// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Auditing;

/// <summary>
/// Holds the causation property names Chronicle recognizes when they are present.
/// </summary>
/// <remarks>
/// A causation's type says what kind of link it is - an HTTP request, a reactor, a command - and every command
/// shares the one type, so the type alone cannot say which command ran. Naming it is what makes behavior mineable:
/// pattern detection reads this property when present and falls back to the causation type when nothing named
/// itself. Arc's command pipeline records it automatically; anything appending events directly can record it too.
/// <para>
/// These names are a wire convention shared with the kernel's own
/// <c>Cratis.Chronicle.Concepts.Patterns.WellKnownCausationProperties</c> and must agree with it, the same way
/// the well-known <see cref="CausationType"/> values do.
/// </para>
/// </remarks>
public static class WellKnownCausationProperties
{
    /// <summary>
    /// The property naming the command a causation link represents.
    /// </summary>
    public const string CommandType = "commandType";
}
