// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Describes whether stored event content equals a prepared append.
/// </summary>
public enum ContentVerificationResult
{
    /// <summary>
    /// The event, requested generation, schema, or fully released content is unavailable.
    /// </summary>
    Unavailable = 0,

    /// <summary>
    /// The complete released content equals the prepared content.
    /// </summary>
    Equal = 1,

    /// <summary>
    /// The event type or complete released content differs.
    /// </summary>
    Different = 2
}
