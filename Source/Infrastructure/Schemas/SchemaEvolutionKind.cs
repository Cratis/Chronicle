// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Describes a schema's relationship to a stored generation.
/// </summary>
public enum SchemaEvolutionKind
{
    /// <summary>The schemas are identical apart from annotations.</summary>
    Identical = 0,

    /// <summary>The change is already tolerated by generation validation.</summary>
    Tolerated = 1,

    /// <summary>Only properties that old payloads can omit were added.</summary>
    Additive = 2,

    /// <summary>An older client supplied a compatible subset of the stored schema.</summary>
    Subset = 3,

    /// <summary>The change requires a new generation.</summary>
    Breaking = 4
}
