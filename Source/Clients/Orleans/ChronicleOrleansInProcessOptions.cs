// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Chronicle;

namespace Orleans.Hosting;

/// <summary>
/// Represents the settings for connecting to Chronicle.
/// </summary>
public class ChronicleOrleansInProcessOptions
{
    /// <summary>
    /// Gets the <see cref="Cratis.Chronicle.EventStoreName"/> to use.
    /// </summary>
    [Required]
    public EventStoreName EventStoreName { get; set; } = string.Empty;
}
