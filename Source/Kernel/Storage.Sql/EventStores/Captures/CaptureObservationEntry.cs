// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.EntityFrameworkCore.Json;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Captures;

/// <summary>
/// Represents the last observed source state for a capture.
/// </summary>
public class CaptureObservationEntry
{
    /// <summary>
    /// Gets or sets the unique identifier of the capture the observation belongs to.
    /// </summary>
    [Key]
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the observed items - key property value to serialized JSON content.
    /// </summary>
    [Json]
    public IDictionary<string, string> Items { get; set; } = new Dictionary<string, string>();
}
