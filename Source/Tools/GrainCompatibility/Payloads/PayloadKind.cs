// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrainCompatibility.Payloads;

/// <summary>
/// The representation a payload uses between silos.
/// </summary>
public enum PayloadKind
{
    /// <summary>
    /// Public JSON properties.
    /// </summary>
    Json = 0,

    /// <summary>
    /// Members identified by Orleans serializer IDs.
    /// </summary>
    Orleans = 1,

    /// <summary>
    /// A strongly typed primitive represented by its underlying value.
    /// </summary>
    Concept = 2,

    /// <summary>
    /// Named numeric values.
    /// </summary>
    Enum = 3,

    /// <summary>
    /// An ordered set of alternative payload types.
    /// </summary>
    OneOf = 4,

    /// <summary>
    /// An opaque representation owned by a custom converter.
    /// </summary>
    Custom = 5
}
