// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compatibility;

namespace Cratis.Chronicle.Tools.WireCompatibility;

/// <summary>
/// One thing that broke, and which released baselines it breaks.
/// </summary>
/// <param name="Incompatibility">What broke.</param>
/// <param name="Baselines">The baseline versions that no longer get served, oldest first.</param>
public record AffectedBaselines(WireIncompatibility Incompatibility, IReadOnlyList<string> Baselines)
{
    /// <summary>
    /// Gets the oldest released baseline this affects.
    /// </summary>
    public string Oldest => Baselines[0];

    /// <summary>
    /// Gets the newest released baseline this affects.
    /// </summary>
    public string Newest => Baselines[^1];

    /// <summary>
    /// Gets a phrase naming the range of releases affected.
    /// </summary>
    public string Range => Baselines.Count == 1 ? Oldest : $"{Oldest} to {Newest}";
}
