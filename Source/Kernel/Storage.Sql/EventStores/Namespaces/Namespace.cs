// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;

/// <summary>
/// Represents a namespace.
/// </summary>
public class Namespace
{
    /// <summary>
    /// Gets or sets the name of the namespace.
    /// </summary>
    [Key]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the creation date of the namespace.
    /// </summary>
    public DateTimeOffset Created { get; set; }
}
