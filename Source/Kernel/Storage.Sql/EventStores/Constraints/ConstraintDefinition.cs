// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.EntityFrameworkCore.Json;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Constraints;

/// <summary>
/// Represents a constraint definition entity for SQL storage.
/// </summary>
public class ConstraintDefinition
{
    /// <summary>
    /// Gets or sets the unique identifier for the persisted constraint version.
    /// </summary>
    [Key]
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the constraint.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the version of the constraint definition.
    /// </summary>
    public ulong Version { get; set; }

    /// <summary>
    /// Gets or sets the CLR type name of the constraint definition.
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Gets or sets the serialized constraint definition as JSON.
    /// </summary>
    [Json]
    public required string Definition { get; set; }
}
