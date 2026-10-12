// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Cratis.Chronicle.Concepts.Observation.Sequencing;

/// <summary>
/// Identifies the domain-separated representation of an unresolved event type id.
/// </summary>
[SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "The canonical format freezes the unresolved form as one byte.")]
public enum UnresolvedKeyForm : byte
{
    /// <summary>
    /// Strict UTF-8 text fitting in the canonical key limit.
    /// </summary>
    Text = 0,

    /// <summary>
    /// SHA-256 of the raw UTF-16 little-endian code units.
    /// </summary>
    Utf16Digest = 1
}
