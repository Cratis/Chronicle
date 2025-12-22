// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.EntityFrameworkCore.Json;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Constraints;

/// <summary>
/// Represents a constraint definition entity for SQL storage.
/// </summary>
public class ConstraintDefinition
{
    /// <summary>
    /// Gets or sets the unique identifier for the constraint (constraint name).
    /// </summary>
    [Key]
    public required string Name { get; set; }

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
