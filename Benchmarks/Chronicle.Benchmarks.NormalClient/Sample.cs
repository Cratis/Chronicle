// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.NormalClient;

/// <summary>One iteration's phase costs.</summary>
public sealed class Sample
{
    /// <summary>Gets or sets reducer discovery cost.</summary>
    public Cost ReducerDiscovery { get; set; }

    /// <summary>Gets or sets projection discovery cost.</summary>
    public Cost ProjectionDiscovery { get; set; }

    /// <summary>Gets or sets reducer dispatch cost.</summary>
    public Cost Dispatch { get; set; }

    /// <summary>Gets or sets release cost.</summary>
    public Cost Release { get; set; }

    /// <summary>Gets or sets the number of discovered reducer handlers.</summary>
    public int Handlers { get; set; }
}
