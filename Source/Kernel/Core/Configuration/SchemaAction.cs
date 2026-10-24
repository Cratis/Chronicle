// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Defines the SQL schema action.
/// </summary>
public enum SchemaAction
{
    /// <summary>
    /// Run the kernel normally.
    /// </summary>
    None = 0,

    /// <summary>
    /// Provision the SQL schema and exit.
    /// </summary>
    MigrateAndExit = 1,

    /// <summary>
    /// Verify the SQL schema and exit.
    /// </summary>
    VerifyAndExit = 2
}
