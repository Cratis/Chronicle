// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Read model registered fluently only by --catch-up-shape; no discovered projection runs in the other benchmarks.
/// </summary>
internal sealed class CatchUpShapeReadModel
{
    /// <summary>
    /// Gets or sets the event source identifier.
    /// </summary>
    [Key]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the latest value.
    /// </summary>
    public int Value { get; set; }
}
