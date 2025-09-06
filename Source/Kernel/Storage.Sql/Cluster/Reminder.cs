// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// Represents a reminder entry.
/// </summary>
public class Reminder
{
    /// <summary>
    /// Gets or sets the unique identifier for the reminder.
    /// </summary>
    [Key]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier for the grain.
    /// </summary>
    public string GrainId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the hash code for the grain identifier.
    /// </summary>
    public uint GrainHash { get; set; }

    /// <summary>
    /// Gets or sets the name of the reminder.
    /// </summary>
    public string ReminderName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the period for the reminder.
    /// </summary>
    public long Period { get; set; }

    /// <summary>
    /// Gets or sets the start time for the reminder.
    /// </summary>
    public long StartAt { get; set; }

    /// <summary>
    /// Gets or sets the ETag for the reminder.
    /// </summary>
    public string ETag { get; set; } = string.Empty;
}
