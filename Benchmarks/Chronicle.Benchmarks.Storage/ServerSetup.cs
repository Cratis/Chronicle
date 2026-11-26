// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>The provider, its server and the durability it ran with.</summary>
/// <param name="Provider">The provider name.</param>
/// <param name="Image">The server image and version.</param>
/// <param name="Topology">The server topology, for example `single node`.</param>
/// <param name="Durability">The durability settings.</param>
public sealed record ServerSetup(string Provider, ServerImage Image, string Topology, DurabilitySettings Durability);
