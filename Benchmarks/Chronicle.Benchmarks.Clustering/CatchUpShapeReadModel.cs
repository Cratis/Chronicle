// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Benchmarks.Clustering;

// Registered fluently only by --catch-up-shape; no discovered projection runs in the other benchmarks.
internal sealed class CatchUpShapeReadModel
{
    [Key]
    public string Id { get; set; } = string.Empty;

    public int Value { get; set; }
}
