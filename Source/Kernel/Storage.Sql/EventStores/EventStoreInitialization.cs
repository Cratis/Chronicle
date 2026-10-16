// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Storage.Sql.EventStores;

/// <summary>
/// Represents the durable completion marker for first-time store initialization.
/// </summary>
public class EventStoreInitialization
{
    /// <summary>
    /// Gets or sets the singleton marker identifier.
    /// </summary>
    [Key]
    public int Id { get; set; }
}
