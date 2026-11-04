// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.ClientStartup;

/// <summary>
/// Records actual handler delivery for one benchmark run.
/// </summary>
public class CatalogDeliveryProbe
{
    /// <summary>
    /// Gets or sets the number of delivered events.
    /// </summary>
    public int Deliveries { get; set; }
}
