// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Storage.Sql.EventStores;

/// <summary>
/// Represents independent durable completion markers for first-time setup and observer enablement.
/// </summary>
public class EventStoreInitialization
{
    /// <summary>
    /// Gets or sets the marker identifier: 1 for first-time kernel setup, 2 for observer enablement.
    /// </summary>
    [Key]
    public int Id { get; set; }
}
