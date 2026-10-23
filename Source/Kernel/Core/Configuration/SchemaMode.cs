// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Defines the SQL schema policy.
/// </summary>
public enum SchemaMode
{
    /// <summary>
    /// Apply pending migrations on access.
    /// </summary>
    Migrate = 0,

    /// <summary>
    /// Only verify the versioned schema; never create databases or schemas.
    /// </summary>
    Verify = 1
}
