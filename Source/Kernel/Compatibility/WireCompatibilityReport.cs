// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compatibility;

/// <summary>
/// Represents the outcome of comparing two versions of the wire contract.
/// </summary>
/// <param name="Incompatibilities">Every way the newer contract fails to serve the older one.</param>
public record WireCompatibilityReport(IReadOnlyList<WireIncompatibility> Incompatibilities)
{
    /// <summary>
    /// Gets a report holding no incompatibilities.
    /// </summary>
    public static WireCompatibilityReport Compatible { get; } = new([]);

    /// <summary>
    /// Gets a value indicating whether the older contract is still served.
    /// </summary>
    public bool IsCompatible => Incompatibilities.Count == 0;
}
