// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>What a provider host read from its running server.</summary>
/// <param name="Image">The server image and version.</param>
/// <param name="Durability">The durability the server runs with.</param>
/// <param name="ConnectionPool">The connection pool setting in effect, or a statement that none applies.</param>
public sealed record ServerFacts(ServerImage Image, DurabilitySettings Durability, string ConnectionPool);
