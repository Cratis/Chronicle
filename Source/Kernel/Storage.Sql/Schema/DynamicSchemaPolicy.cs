// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Schema;

/// <summary>
/// The internal policy applied immediately before dynamic schema changes.
/// </summary>
internal enum DynamicSchemaPolicy
{
    /// <summary>
    /// Permits runtime DDL without a warning.
    /// </summary>
    Allow = 0,

    /// <summary>
    /// Permits DDL and reports each newly encountered object.
    /// </summary>
    Warn = 1,

    /// <summary>
    /// Rejects DDL for an object requiring provisioning.
    /// </summary>
    Refuse = 2
}
