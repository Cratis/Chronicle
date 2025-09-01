// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Represents an event store.
/// </summary>
public class EventStore
{
    /// <summary>
    /// Gets or sets the name of the event store.
    /// </summary>
    [Key]
    public required string Name { get; set; }
}
