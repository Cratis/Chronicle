// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Cratis.Chronicle.Concepts.Observation.Sequencing;

/// <summary>
/// Identifies the domain of a canonical sequencing key.
/// </summary>
[SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "The canonical format freezes the kind as one byte.")]
public enum SequencingKeyKind : byte
{
    /// <summary>
    /// An ordinal tuple of text values.
    /// </summary>
    Value = 0,

    /// <summary>
    /// A key for events whose value key could not be resolved.
    /// </summary>
    Unresolved = 1,

    /// <summary>
    /// A default event source id key whose text cannot be a Value key.
    /// </summary>
    EventSourceIdDigest = 2
}
