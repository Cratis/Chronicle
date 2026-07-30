// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Captures;

/// <summary>
/// Represents a single capture.
/// </summary>
public class Capture
{
    /// <summary>
    /// Gets or sets the unique identifier for the capture.
    /// </summary>
    [Key]
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the capture.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the capture declaration language source text.
    /// </summary>
    public string Declaration { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the <see cref="CaptureStatus"/>.
    /// </summary>
    public CaptureStatus Status { get; set; }
}
