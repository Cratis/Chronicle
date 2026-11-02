// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>The scalable workload shape; event counts are per store/namespace pair.</summary>
/// <param name="Stores">The store count.</param>
/// <param name="Namespaces">Namespaces per store.</param>
/// <param name="SeedEvents">Seed events per pair.</param>
/// <param name="Events">Measured events per pair.</param>
/// <param name="BatchSize">Events per append.</param>
/// <param name="Appenders">The global append concurrency.</param>
internal sealed record WorkloadSettings(int Stores = 3, int Namespaces = 5, int SeedEvents = 200, int Events = 30000, int BatchSize = 100, int Appenders = 4);
