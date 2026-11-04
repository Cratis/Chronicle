// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Security;

/// <summary>
/// Represents a separately persisted administrator privilege.
/// </summary>
[Table(WellKnownTableNames.Administrators)]
public class AdministratorEntity
{
    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the grant reason.
    /// </summary>
    public int Reason { get; set; }

    /// <summary>
    /// Gets or sets the supporting audit evidence.
    /// </summary>
    public string Evidence { get; set; } = string.Empty;
}
