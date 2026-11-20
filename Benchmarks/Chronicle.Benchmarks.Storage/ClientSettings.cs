// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>Client-side settings that affect what a result means.</summary>
/// <param name="ConnectionPool">The connection pool setting in effect as the client reports it, or a statement that none applies.</param>
/// <param name="SnapshotInterval">The event sequence snapshot interval the activation workload depends on.</param>
/// <param name="CountersEnabled">Whether command counting was on (it adds overhead).</param>
public sealed record ClientSettings(string ConnectionPool, int SnapshotInterval, bool CountersEnabled);
