// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Security;

/// <summary>
/// Represents the EF entity for a Data Protection key.
/// </summary>
public class DataProtectionKeyEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the key.
    /// </summary>
    [Key]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the friendly name of the key.
    /// </summary>
    public string FriendlyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the XML representation of the key.
    /// </summary>
    public string Xml { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the key was created.
    /// </summary>
    public DateTimeOffset Created { get; set; }
}
