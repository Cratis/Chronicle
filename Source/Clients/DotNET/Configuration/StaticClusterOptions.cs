// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Configuration;

/// <summary>
/// Represents the configuration options for a static cluster.
/// </summary>
public class StaticClusterOptions
{
    public IEnumerable<Uri> Endpoints { get; init; } = Enumerable.Empty<Uri>();
}

