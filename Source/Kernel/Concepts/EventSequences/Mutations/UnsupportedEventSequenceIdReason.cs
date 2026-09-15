// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.EventSequences.Mutations;

/// <summary>
/// Specifies why an event sequence identifier cannot be represented as a mutation identity.
/// </summary>
public enum UnsupportedEventSequenceIdReason
{
    /// <summary>
    /// The identifier value is missing.
    /// </summary>
    MissingValue = 0,

    /// <summary>
    /// The identifier contains ill-formed UTF-16.
    /// </summary>
    IllFormedUtf16 = 1,

    /// <summary>
    /// The identifier contains a NUL character.
    /// </summary>
    ContainsNul = 2,

    /// <summary>
    /// The identifier exceeds the supported UTF-16 or UTF-8 length.
    /// </summary>
    TooLong = 3
}
