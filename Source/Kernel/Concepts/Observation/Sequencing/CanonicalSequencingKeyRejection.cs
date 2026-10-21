// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing;

/// <summary>
/// Identifies why a logical key has no canonical value representation.
/// </summary>
public enum CanonicalSequencingKeyRejection
{
    /// <summary>
    /// The key was accepted.
    /// </summary>
    None = 0,

    /// <summary>
    /// The key or its parts are invalid.
    /// </summary>
    InvalidKey = 1,

    /// <summary>
    /// A value part is not well-formed UTF-16.
    /// </summary>
    InvalidText = 2,

    /// <summary>
    /// The value key exceeds the canonical byte limit.
    /// </summary>
    TooLarge = 3
}
